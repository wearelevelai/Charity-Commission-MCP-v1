using System.Text.Json;
using CCEW.Mcp.ContractTests.TestUtils;
using FluentAssertions;

namespace CCEW.Mcp.ContractTests.Schemas;

public class EnrichmentSchemaTests
{
    private const string ContractsRoot = "specs/001-provide-an-mcp/contracts";

    [Fact]
    public void Get_content_by_id_output_should_allow_enrichment_flag()
    {
        using var doc = SchemaLoader.ReadJson(Path.Combine(ContractsRoot, "get_content_by_id.output.schema.json"));
        var root = doc.RootElement;
        var props = root.GetProperty("properties");
        props.TryGetProperty("enrichment", out _).Should().BeTrue();
        var enrichmentProps = props.GetProperty("enrichment").GetProperty("properties");
        enrichmentProps.TryGetProperty("is_enrichment", out _).Should().BeTrue();
    }

    [Fact]
    public void Get_content_by_path_output_should_allow_enrichment_flag()
    {
        using var doc = SchemaLoader.ReadJson(Path.Combine(ContractsRoot, "get_content_by_path.output.schema.json"));
        var root = doc.RootElement;
        var props = root.GetProperty("properties");
        props.TryGetProperty("enrichment", out _).Should().BeTrue();
        var enrichmentProps = props.GetProperty("enrichment").GetProperty("properties");
        enrichmentProps.TryGetProperty("is_enrichment", out _).Should().BeTrue();
    }
}
