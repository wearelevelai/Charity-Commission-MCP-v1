using System.Net;
using System.Net.Http.Json;
using CCEW.Mcp.ContractTests.TestUtils;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class GetContentByIdUpstreamMockTests : IClassFixture<MockedFactory>
{
    private readonly MockedFactory _factory;

    public GetContentByIdUpstreamMockTests(MockedFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetContentById_Should_ResolveViaSearch_ThenFetchContent()
    {
        // Arrange upstream: search by content_id returns one item with link
        _factory.Server.Reset();
        var contentId = "11111111-1111-1111-1111-111111111111";
        var searchResponse = new
        {
            total = 1,
            results = new object[]
            {
                new { title = "X", link = "/guidance/test-item", description = "desc", public_timestamp = "2022-01-01T00:00:00Z", content_id = contentId }
            }
        };
        _factory.Server.Given(Request.Create().WithPath("/api/search.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(searchResponse));

        // Arrange upstream: content fetch returns GOV.UK content JSON
        var contentResponse = new
        {
            base_path = "/guidance/test-item",
            public_updated_at = "2022-01-02T00:00:00Z",
            content_id = contentId,
            title = "X"
        };
        _factory.Server.Given(Request.Create().WithPath("/api/content/guidance/test-item").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(contentResponse));

        // Act
        using var client = _factory.CreateClient();
        var body = new { content_id = contentId };
        var rsp = await client.PostAsJsonAsync("/tools/get_content_by_id", body);

        // Assert
        rsp.EnsureSuccessStatusCode();
        var json = await rsp.Content.ReadFromJsonAsync<Out>();
        json.Should().NotBeNull();
        json!.Url.Should().Be("https://www.gov.uk/guidance/test-item");
        json.Public_Updated_At.Should().Be("2022-01-02T00:00:00Z");
        json.Content_Id.Should().Be(contentId);
        json.Attribution.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetContentById_Strict_Should_Map404()
    {
        _factory.Server.Reset();
        // Search returns empty results
        var searchResponse = new { total = 0, results = Array.Empty<object>() };
        _factory.Server.Given(Request.Create().WithPath("/api/search.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(searchResponse));

        using var client = _factory.CreateClient();
        var body = new { content_id = "22222222-2222-2222-2222-222222222222", options = new { strict_upstream_errors = true } };
        var rsp = await client.PostAsJsonAsync("/tools/get_content_by_id", body);
        rsp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var err = await rsp.Content.ReadFromJsonAsync<Err>();
        err!.Code.Should().Be("NOT_FOUND_OR_REDIRECTED");
    }

    private record Out(object Content, string Url, string Public_Updated_At, string Attribution, string? Content_Id);
    private record Err(string Code, string Error);
}
