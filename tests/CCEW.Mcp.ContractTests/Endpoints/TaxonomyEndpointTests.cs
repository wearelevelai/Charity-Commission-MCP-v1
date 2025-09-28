using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class TaxonomyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TaxonomyEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_error_taxonomy_should_include_all_required_codes()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.GetFromJsonAsync<TaxonomyResponse>("/tools/get_error_taxonomy");
        rsp.Should().NotBeNull();
        rsp!.Errors.Should().NotBeNull();

        var codes = rsp.Errors.Select(e => e.Code).ToHashSet();
        var expected = new[]
        {
            "UPSTREAM_RATE_LIMITED",
            "NOT_FOUND_OR_REDIRECTED",
            "STALE_CACHE_SERVED",
            "CONTENT_OUT_OF_SCOPE",
            "UPSTREAM_PARAMETER_ERROR"
        };
        codes.Should().Contain(expected);
    }

    private record TaxonomyResponse(TaxonomyError[] Errors);
    private record TaxonomyError(string Code, string Description);
}
