using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class ForceRefreshAndAttributionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ForceRefreshAndAttributionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Force_refresh_should_return_ok_and_not_cached()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/tools/force_refresh", new { reason = "test" });
        rsp.EnsureSuccessStatusCode();
        var body = await rsp.Content.ReadFromJsonAsync<ForceRefreshResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("ok");
        body.Cached.Should().BeFalse();
    }

    [Theory]
    [InlineData("/tools/get_content_by_path", "post")]
    [InlineData("/tools/get_content_by_id", "post")]
    public async Task Content_endpoints_should_include_attribution(string path, string method)
    {
        using var client = _factory.CreateClient();
        HttpResponseMessage rsp = method == "post"
            ? await client.PostAsJsonAsync(path, new { dummy = true })
            : await client.GetAsync(path);
        rsp.EnsureSuccessStatusCode();
        var json = await rsp.Content.ReadFromJsonAsync<ContentEnvelope>();
        json.Should().NotBeNull();
        json!.Attribution.Should().NotBeNullOrWhiteSpace();
    }

    private record ForceRefreshResponse(string Status, bool Cached);
    private record ContentEnvelope(object Content, string Url, string Public_Updated_At, string Attribution, string? Content_Id);
}
