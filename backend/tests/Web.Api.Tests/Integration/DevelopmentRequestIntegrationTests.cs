using System.Net;
using System.Text;
using Web.Api.Middlewares;

namespace Web.Api.Tests.Integration;

/// <summary>
/// The shared host runs in Development, where minimal APIs throw for a request they can't bind instead of answering
/// 400 themselves. These check that such a request still gets a 400, not a 500.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class DevelopmentRequestIntegrationTests(PostgresApiFixture api)
{
    private readonly HttpClient client = api.CreateClient();

    [Theory]
    [InlineData("/brands?page=abc")]
    [InlineData("/products/prices?productId=not-an-id")]
    public async Task InvalidQueryValue_ReturnsBadRequest(string path)
    {
        var response = await client.GetAsync(path);

        await AssertBadRequestAsync(response);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"name":123}""")]
    public async Task MalformedJson_ReturnsBadRequest(string body)
    {
        var response = await client.PostAsync("/brands", new StringContent(body, Encoding.UTF8, "application/json"));

        await AssertBadRequestAsync(response);
    }

    private static async Task AssertBadRequestAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(GlobalExceptionHandler.BadRequestCode,
            (await response.ReadJsonAsync()).GetProperty("title").GetString());
    }
}
