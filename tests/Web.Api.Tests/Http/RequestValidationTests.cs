using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tests.Common;
using Web.Api.Infrastructure;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Http;

public class RequestValidationTests(NoDatabaseApiFixture api) : IClassFixture<NoDatabaseApiFixture>
{
    private const string SomeId = "11111111-1111-1111-1111-111111111111";
    private const string When = "2026-01-15T10:00:00Z";

    private static readonly string ZeroQuantityBody = JsonSerializer.Serialize(new
    {
        title = "t",
        tags = Array.Empty<string>(),
        productEntries = new[] { new { id = SomeId, quantity = 0 } },
        occurrenceTime = When,
    });

    public static TheoryData<string, string, string, string> InvalidBodies => new()
    {
        { "POST", "/brands", """{"name":""}""", "Name" },
        { "PUT", "/brands/1", """{"name":""}""", "Name" },
        { "POST", "/products/tags", """{"name":""}""", "Name" },
        { "PUT", "/products/tags/1", """{"name":""}""", "Name" },
        { "POST", "/products/units", """{"name":""}""", "Name" },
        { "PUT", "/products/units/1", """{"name":""}""", "Name" },
        { "POST", $"/products/{SomeId}/prices", """{"value":-1}""", "Value" },
        { "PUT", $"/products/prices/{SomeId}", """{"value":-1}""", "Value" },
        {
            "POST", "/products",
            """{"name":"","brandId":1,"measure":1,"measureUnitId":1,"price":1,"tags":[]}""", "Name"
        },
        {
            "POST", "/products",
            """{"name":"x","brandId":0,"measure":1,"measureUnitId":1,"price":1,"tags":[]}""", "BrandId"
        },
        {
            "POST", "/products",
            """{"name":"x","brandId":1,"measure":-1,"measureUnitId":1,"price":1,"tags":[]}""", "Measure"
        },
        {
            "POST", "/products",
            """{"name":"x","brandId":1,"measure":1,"measureUnitId":0,"price":1,"tags":[]}""", "UnitId"
        },
        {
            "POST", "/products",
            """{"name":"x","brandId":1,"measure":1,"measureUnitId":1,"price":-1,"tags":[]}""", "Price"
        },
        {
            "POST", "/products",
            """{"name":"x","brandId":1,"measure":1,"measureUnitId":1,"price":1,"tags":[""]}""", "Tags"
        },
        {
            "POST", "/purchases",
            $$"""{"title":"","tags":[],"productEntries":[],"occurrenceTime":"{{When}}"}""", "Title"
        },
        {
            "POST", "/purchases",
            ZeroQuantityBody,
            "ProductEntries"
        },
        {
            "PUT", $"/purchases/{SomeId}/details",
            $$"""{"title":"","tags":[],"occurrenceTime":"{{When}}"}""", "Title"
        },
        {
            "PUT", $"/purchases/{SomeId}/entries",
            $$"""{"productEntries":[{"productId":"{{SomeId}}","quantity":0}]}""", "ProductEntries"
        },
        {
            "POST", "/sales",
            $$"""{"title":"","tags":[],"productEntries":[],"occurrenceTime":"{{When}}"}""", "Title"
        },
        {
            "POST", "/sales",
            ZeroQuantityBody,
            "ProductEntries"
        },
        {
            "PUT", $"/sales/{SomeId}/details",
            $$"""{"title":"","tags":[],"occurrenceTime":"{{When}}"}""", "Title"
        },
        {
            "PUT", $"/sales/{SomeId}/entries",
            $$"""{"productEntries":[{"productId":"{{SomeId}}","quantity":0}]}""", "ProductEntries"
        },
        { "POST", "/roles", """{"name":"ab"}""", "Role" },
        { "PUT", "/roles/1", """{"name":"ab"}""", "Name" },
        { "POST", "/users/login", """{"email":"not-an-email","password":"x"}""", "Email" },
        { "POST", "/users/login", """{"email":"a@b.com","password":""}""", "Password" },
        {
            "POST", "/users/register",
            """{"email":"not-an-email","password":"Passw0rd!","firstName":"Jane"}""", "Email"
        },
        { "POST", "/users/register", """{"email":"a@b.com","password":"weak","firstName":"Jane"}""", "Password" },
        { "POST", "/users/register", """{"email":"a@b.com","password":"Passw0rd!","firstName":"Ja"}""", "FirstName" },
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task InvalidBody_ReturnsBadRequestWithFieldError(string method, string path, string body, string field)
    {
        var response = await SendJsonAsync(method, path, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Validation.General", problem.GetProperty("title").GetString());
        var fieldErrors = problem.GetProperty(CustomHttpResults.ValidationErrorFieldName);
        var camelCaseField = JsonNamingPolicy.CamelCase.ConvertName(field);
        Assert.True(fieldErrors.TryGetProperty(camelCaseField, out var errors), fieldErrors.ToString());
        Assert.NotEqual(0, errors.GetArrayLength());
    }

    [Theory]
    [InlineData("POST", "/brands", "{not json")]
    [InlineData("POST", "/users/register", "[1,2,3]")]
    [InlineData("POST", "/products/tags", """{"name":123}""")]
    [InlineData("PUT", "/brands/1", """{"name":""")]
    [InlineData("POST", "/products", """{"name":"x","brandId":"abc"}""")]
    [InlineData("POST", "/purchases", """{"occurrenceTime":"not-a-date"}""")]
    [InlineData("PUT", $"/sales/{SomeId}/entries", """{"productEntries":[{"productId":"not-a-guid"}]}""")]
    public async Task MalformedJson_ReturnsBadRequest(string method, string path, string body)
    {
        var response = await SendJsonAsync(method, path, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/brands")]
    [InlineData("/users/login")]
    public async Task EmptyBody_ReturnsBadRequest(string path)
    {
        var response = await SendJsonAsync("POST", path, string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WrongContentType_ReturnsUnsupportedMediaType()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/brands")
        {
            Content = new StringContent("name=x", Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        var response = await api.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task InvalidQueryValue_ReturnsBadRequest()
    {
        var response = await api.CreateClient().GetAsync("/brands?page=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [KnownBugFact("Validators throw on a missing tags array, so the request fails with a 500 instead of a 400")]
    public async Task MissingTags_ReturnsBadRequest()
    {
        var response = await SendJsonAsync("POST", "/products",
            """{"name":"x","brandId":1,"measure":1,"measureUnitId":1,"price":1}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [KnownBugFact("Validators throw on a missing productEntries array, so the request gives a 500, not a 400")]
    public async Task MissingProductEntries_ReturnsBadRequest()
    {
        var response = await SendJsonAsync("POST", "/purchases",
            $$"""{"title":"t","tags":[],"occurrenceTime":"{{When}}"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DatabaseFailureInsideHandler_ReturnsSystemProblem_WithoutInternalDetails()
    {
        var response = await api.CreateClient().GetAsync("/brands");

        await AssertSystemProblemAsync(response);
    }

    [Fact]
    public async Task DatabaseFailureOutsideHandlerTryBlock_ReachesGlobalExceptionHandler()
    {
        var response = await SendJsonAsync("POST", "/brands", """{"name":"Acme"}""");

        await AssertSystemProblemAsync(response);
    }

    private static async Task AssertSystemProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(CustomHttpResults.SystemErrorTitle, body);
        Assert.DoesNotContain("Host=", body);
        Assert.DoesNotContain("localhost", body);
        Assert.DoesNotContain("Exception", body);
    }

    private async Task<HttpResponseMessage> SendJsonAsync(string method, string path, string body)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        return await api.CreateClient().SendAsync(request);
    }
}
