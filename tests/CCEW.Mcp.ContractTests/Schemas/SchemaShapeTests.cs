using System.Text.Json;
using CCEW.Mcp.ContractTests.TestUtils;
using FluentAssertions;

namespace CCEW.Mcp.ContractTests.Schemas;

public class SchemaShapeTests
{
    private const string ContractsRoot = "specs/001-provide-an-mcp/contracts";

    [Fact]
    public void Search_output_items_should_have_expected_fields()
    {
        using var doc = SchemaLoader.ReadJson(Path.Combine(ContractsRoot, "search_guidance.output.schema.json"));
        var root = doc.RootElement;
        var resultsItemsProps = root.GetProperty("properties").GetProperty("results").GetProperty("items").GetProperty("properties");

        resultsItemsProps.TryGetProperty("title", out _).Should().BeTrue();
        resultsItemsProps.TryGetProperty("url", out _).Should().BeTrue();
        resultsItemsProps.TryGetProperty("summary", out _).Should().BeTrue();
        resultsItemsProps.TryGetProperty("public_updated_at", out _).Should().BeTrue();
        resultsItemsProps.TryGetProperty("content_id", out _).Should().BeTrue();

        var required = root.GetProperty("properties").GetProperty("results").GetProperty("items").GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToHashSet();
        required.Should().Contain(new[] { "title", "url", "public_updated_at" });
    }

    [Fact]
    public void Get_content_by_id_output_should_require_core_fields()
    {
        using var doc = SchemaLoader.ReadJson(Path.Combine(ContractsRoot, "get_content_by_id.output.schema.json"));
        var root = doc.RootElement;

        var required = root.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToHashSet();
        var expected = new[] { "content", "url", "public_updated_at", "attribution", "content_id" };
        required.Should().Contain(expected);

        var props = root.GetProperty("properties");
        props.TryGetProperty("content", out _).Should().BeTrue();
        props.TryGetProperty("attribution", out _).Should().BeTrue();
        props.TryGetProperty("content_id", out _).Should().BeTrue();
    }
}
