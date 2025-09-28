using System.Diagnostics;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Performance;

public class SearchP95Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SearchP95Tests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task SearchGuidance_P95_Under600ms()
    {
        var http = _factory.CreateClient();
        var samples = new List<long>();

        var body = new
        {
            query = "charity",
            page = 1,
            pageSize = 5,
            filters = new { organisation = "charity-commission" }
        };

        // Warm-up
        var warm = await http.PostAsJsonAsync("/tools/search_guidance", body);
        warm.EnsureSuccessStatusCode();

        for (int i = 0; i < 10; i++)
        {
            var sw = Stopwatch.StartNew();
            var resp = await http.PostAsJsonAsync("/tools/search_guidance", body);
            sw.Stop();
            resp.EnsureSuccessStatusCode();
            samples.Add(sw.ElapsedMilliseconds);
        }

        samples.Sort();
        var p95Index = (int)Math.Ceiling(0.95 * samples.Count) - 1;
        p95Index = Math.Clamp(p95Index, 0, samples.Count - 1);
        var p95 = samples[p95Index];

        // Budget: 600ms p95 on CI for search with small page size
        p95.Should().BeLessThan(600, $"p95 should be < 600ms but was {p95}ms; samples=[{string.Join(",", samples)}]");
    }
}
