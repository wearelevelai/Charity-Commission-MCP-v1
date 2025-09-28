using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class EnrichmentBehaviorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EnrichmentBehaviorTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetContentById_ShouldExposeOptionalEnrichmentObject()
    {
        var payload = new
        {
            content_id = "test-content-id-123",
            options = new { include_enrichment = true }
        };

    using var client = _factory.CreateClient();
    var resp = await client.PostAsJsonAsync("/tools/get_content_by_id", payload);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("enrichment", out var enrichment).Should().BeTrue("when include_enrichment=true is requested, enrichment should be present");
        enrichment.GetProperty("is_enrichment").GetBoolean().Should().BeTrue();

        root.TryGetProperty("content", out var content).Should().BeTrue();
        content.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task GetContentByPath_ShouldExposeOptionalEnrichmentObject()
    {
        var payload = new
        {
            path = "/government/organisations/charity-commission",
            options = new { include_enrichment = true }
        };

    using var client = _factory.CreateClient();
    var resp = await client.PostAsJsonAsync("/tools/get_content_by_path", payload);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("enrichment", out var enrichment).Should().BeTrue();
        enrichment.GetProperty("is_enrichment").GetBoolean().Should().BeTrue();

        root.TryGetProperty("content", out var content).Should().BeTrue();
        content.ValueKind.Should().Be(JsonValueKind.Object);
    }
}
