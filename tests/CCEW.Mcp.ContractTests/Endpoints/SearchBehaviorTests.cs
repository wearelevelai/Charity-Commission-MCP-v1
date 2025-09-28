using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class SearchBehaviorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SearchBehaviorTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_default_should_return_page_1_size_20()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/tools/search_guidance", new { query = "governance" });
        rsp.EnsureSuccessStatusCode();
        var body = await rsp.Content.ReadFromJsonAsync<SearchResponse>();
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(20);
        body.Results.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_pageSize_capped_at_100()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/tools/search_guidance", new { query = "charity", pageSize = 1000 });
        rsp.EnsureSuccessStatusCode();
        var body = await rsp.Content.ReadFromJsonAsync<SearchResponse>();
        body.Should().NotBeNull();
        body!.PageSize.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public async Task Search_empty_query_should_return_400()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/tools/search_guidance", new { query = "" });
        rsp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record SearchResponse(SearchItem[] Results, int Page, int PageSize, int Total);
    private record SearchItem(string Title, string Url, string Summary, string Public_Updated_At, string? Content_Id);

    [Fact]
    public async Task Invalid_filter_date_should_map_to_upstream_parameter_error()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/tools/search_guidance", new { query = "charity", filters = new { public_timestamp_from = "not-a-date" } });
        rsp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await rsp.Content.ReadFromJsonAsync<ErrorResponse>();
        json.Should().NotBeNull();
        json!.Code.Should().Be("UPSTREAM_PARAMETER_ERROR");
    }

    [Fact]
    public async Task Repeated_calls_within_window_should_have_stable_ordering()
    {
        using var client = _factory.CreateClient();
        var body = new { query = "charity governance", page = 1, pageSize = 20 };
        var r1 = await client.PostAsJsonAsync("/tools/search_guidance", body);
        var r2 = await client.PostAsJsonAsync("/tools/search_guidance", body);
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();
        var a = await r1.Content.ReadFromJsonAsync<SearchResponse>();
        var b = await r2.Content.ReadFromJsonAsync<SearchResponse>();
        a!.Results.Should().BeEquivalentTo(b!.Results);
    }

    private record ErrorResponse(string Code, string? Error = null);
}
