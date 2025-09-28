using CCEW.Mcp.ContractTests.TestUtils;
using FluentAssertions;
using NJsonSchema;

namespace CCEW.Mcp.ContractTests.Schemas;

public class SchemaExistenceAndValidityTests
{
    private const string ContractsRoot = "specs/001-provide-an-mcp/contracts";

    public static IEnumerable<object[]> SchemaFiles()
    {
        yield return new object[] { "search_guidance.input.schema.json" };
        yield return new object[] { "search_guidance.output.schema.json" };
        yield return new object[] { "get_content_by_path.input.schema.json" };
        yield return new object[] { "get_content_by_path.output.schema.json" };
        yield return new object[] { "get_content_by_id.input.schema.json" };
        yield return new object[] { "get_content_by_id.output.schema.json" };
        yield return new object[] { "get_source_metadata.input.schema.json" };
        yield return new object[] { "get_source_metadata.output.schema.json" };
        yield return new object[] { "force_refresh.input.schema.json" };
        yield return new object[] { "force_refresh.output.schema.json" };
        yield return new object[] { "get_error_taxonomy.output.schema.json" };
    }

    [Theory]
    [MemberData(nameof(SchemaFiles))]
    public async Task Schemas_should_parse_as_valid_json_schema(string fileName)
    {
        var fullPath = Path.Combine(ContractsRoot, fileName);
        var json = SchemaLoader.ReadText(fullPath);

        var schema = await JsonSchema.FromJsonAsync(json);

        schema.Should().NotBeNull();
        // Basic sanity: type should generally be object at the top level
        schema.Type.HasFlag(NJsonSchema.JsonObjectType.Object).Should().BeTrue(
            $"Top-level schema in {fileName} should be an object");
    }
}
