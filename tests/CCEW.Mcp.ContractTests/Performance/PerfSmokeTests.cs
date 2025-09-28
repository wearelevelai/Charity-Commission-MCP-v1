using System.Diagnostics;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Performance;

public class PerfSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PerfSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task GetErrorTaxonomy_P95_Under500ms()
    {
        var http = _factory.CreateClient();
        var samples = new List<long>();

        // Warm-up
        var warm = await http.GetAsync("/tools/get_error_taxonomy");
        warm.EnsureSuccessStatusCode();

        for (int i = 0; i < 15; i++)
        {
            var sw = Stopwatch.StartNew();
            var resp = await http.GetAsync("/tools/get_error_taxonomy");
            sw.Stop();
            resp.EnsureSuccessStatusCode();
            samples.Add(sw.ElapsedMilliseconds);
        }

        samples.Sort();
        var p95Index = (int)Math.Ceiling(0.95 * samples.Count) - 1;
        p95Index = Math.Clamp(p95Index, 0, samples.Count - 1);
        var p95 = samples[p95Index];

        // Budget: 500ms on CI runners for a lightweight endpoint
        p95.Should().BeLessThan(500, $"p95 should be < 500ms but was {p95}ms; samples=[{string.Join(",", samples)}]");
    }
}
