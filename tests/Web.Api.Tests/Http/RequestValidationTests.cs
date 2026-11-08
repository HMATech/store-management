using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    private static readonly string NegativeUnitCostBody = JsonSerializer.Serialize(new
    {
        title = "t",
        tags = Array.Empty<string>(),
        productEntries = new[] { new { id = SomeId, quantity = 1, unitCost = -1m } },
        occurrenceTime = When,
    });

    private static readonly string NegativeMinimumProductBody = JsonSerializer.Serialize(new
    {
        name = "x",
        brandId = 1,
        measure = 1,
        measureUnitId = 1,
        price = 1,
        tags = Array.Empty<string>(),
        stockDefinition = new { minimumQuantity = -1 },
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
            "POST", "/products",
            NegativeMinimumProductBody,
            "StockDefinition.MinimumQuantity"
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
            "POST", "/purchases",
            NegativeUnitCostBody,
            "ProductEntries"
        },
        {
            "PUT", $"/purchases/{SomeId}/entries",
            $$"""{"productEntries":[{"productId":"{{SomeId}}","quantity":1,"unitCost":-1}]}""", "ProductEntries"
        },
        {
            "POST", "/sales",
            ZeroQuantityBody,
            "ProductEntries"
        },
        {
            "PUT", $"/sales/{SomeId}/details",
            """{"tags":[],"occurrenceTime":"0001-01-01T00:00:00Z"}""", "OccurrenceTime"
        },
        {
            "PUT", $"/sales/{SomeId}/entries",
            $$"""{"productEntries":[{"productId":"{{SomeId}}","quantity":0}]}""", "ProductEntries"
        },
        {
            "POST", "/stocks/adjustments",
            $$"""{"productId":"{{SomeId}}","quantity":0,"reason":"Count","occurrenceTime":"{{When}}"}""", "Quantity"
        },
        {
            "POST", "/stocks/adjustments",
            $$"""{"productId":"{{SomeId}}","quantity":1,"reason":"","occurrenceTime":"{{When}}"}""", "Reason"
        },
        {
            "POST", "/stocks/adjustments",
            $$"""{"quantity":1,"reason":"Count","occurrenceTime":"{{When}}"}""", "ProductId"
        },
        {
            "POST", "/stocks/adjustments",
            $$"""{"productId":"{{SomeId}}","quantity":1,"reason":"Count"}""", "OccurrenceTime"
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
    [InlineData("POST", "/stocks/adjustments", """{"productId":"not-a-guid","quantity":1}""")]
    [InlineData("POST", "/stocks/adjustments", """{"quantity":"abc"}""")]
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

    [Fact]
    public async Task MissingTags_ReturnsBadRequest()
    {
        var response = await SendJsonAsync("POST", "/products",
            """{"name":"x","brandId":1,"measure":1,"measureUnitId":1,"price":1}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingProductEntries_ReturnsBadRequest()
    {
        var response = await SendJsonAsync("POST", "/purchases",
            $$"""{"title":"t","tags":[],"occurrenceTime":"{{When}}"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("PUT", $"/products/{SomeId}", """{"name":"x","measure":1,"measureUnitId":1,"price":1}""")]
    [InlineData("POST", "/purchases", """{"title":"t","productEntries":[],"occurrenceTime":"2026-01-15T10:00:00Z"}""")]
    [InlineData("POST", "/sales", """{"title":"t","productEntries":[],"occurrenceTime":"2026-01-15T10:00:00Z"}""")]
    [InlineData("POST", "/sales", """{"title":"t","tags":[],"occurrenceTime":"2026-01-15T10:00:00Z"}""")]
    [InlineData("PUT", $"/purchases/{SomeId}/details", """{"title":"t","occurrenceTime":"2026-01-15T10:00:00Z"}""")]
    [InlineData("PUT", $"/sales/{SomeId}/details", """{"title":"t","occurrenceTime":"2026-01-15T10:00:00Z"}""")]
    [InlineData("PUT", $"/purchases/{SomeId}/entries", "{}")]
    [InlineData("PUT", $"/sales/{SomeId}/entries", "{}")]
    public async Task MissingRequiredArray_ReturnsBadRequest(string method, string path, string body)
    {
        var response = await SendJsonAsync(method, path, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/brands", "page=0", "page")]
    [InlineData("/brands", "page=-1", "page")]
    [InlineData("/brands", "pageSize=0", "pageSize")]
    [InlineData("/brands", "pageSize=-1", "pageSize")]
    [InlineData("/products", "page=0", "page")]
    [InlineData("/products", "pageSize=0", "pageSize")]
    [InlineData("/products/prices", "page=0", "page")]
    [InlineData("/products/prices", "pageSize=0", "pageSize")]
    [InlineData("/purchases", "page=0", "page")]
    [InlineData("/purchases", "pageSize=0", "pageSize")]
    [InlineData("/sales", "page=0", "page")]
    [InlineData("/sales", "pageSize=0", "pageSize")]
    [InlineData("/stocks", "page=0", "page")]
    [InlineData("/stocks", "pageSize=0", "pageSize")]
    [InlineData("/stocks/adjustments", "page=0", "page")]
    [InlineData("/stocks/adjustments", "pageSize=0", "pageSize")]
    public async Task InvalidPaging_ReturnsBadRequestWithFieldError(string path, string query, string field)
    {
        var response = await api.CreateClient().GetAsync($"{path}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty(CustomHttpResults.ValidationErrorFieldName).TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("/stocks?status=1", "status")]
    [InlineData("/stocks?status=empty", "status")]
    [InlineData("/products?stockStatus=1", "stockStatus")]
    [InlineData("/products?stockStatus=sold-out", "stockStatus")]
    public async Task UnknownStockStatus_ReturnsBadRequest(string path, string field)
    {
        var response = await api.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty(CustomHttpResults.ValidationErrorFieldName).TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("to=2026-01-10", "from")]
    [InlineData("from=2026-01-10", "to")]
    [InlineData("from=2026-01-10&to=2026-01-09", "to")]
    [InlineData("from=2026-01-10&to=2026-01-10&timeZone=Mars/Olympus", "timeZone")]
    public async Task InvalidDailyTotalsQuery_ReturnsBadRequestWithFieldError(string query, string field)
    {
        var response = await api.CreateClient().GetAsync($"/reports/daily-totals?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty(CustomHttpResults.ValidationErrorFieldName).TryGetProperty(field, out _));
    }

    [Fact]
    public async Task DailyTotals_WithMalformedDate_ReturnsBadRequest()
    {
        var response = await api.CreateClient().GetAsync("/reports/daily-totals?from=yesterday&to=2026-01-10");

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
