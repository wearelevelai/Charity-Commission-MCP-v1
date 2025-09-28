using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class ContentPathAndMetadataTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ContentPathAndMetadataTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_content_by_path_should_include_provenance_fields()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/tools/get_content_by_path", new { path = "/some/path" });
        rsp.EnsureSuccessStatusCode();
        var body = await rsp.Content.ReadFromJsonAsync<GetContentResponse>();
        body.Should().NotBeNull();
        body!.Url.Should().NotBeNullOrWhiteSpace();
        body.Public_Updated_At.Should().NotBeNullOrWhiteSpace();
        body.Attribution.Should().NotBeNullOrWhiteSpace();
        body.Disclaimer.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_source_metadata_should_return_expected_metadata()
    {
        using var client = _factory.CreateClient();
        var rsp = await client.GetAsync("/tools/get_source_metadata");
        rsp.EnsureSuccessStatusCode();
        var meta = await rsp.Content.ReadFromJsonAsync<SourceMetadata>();
        meta.Should().NotBeNull();
        meta!.Organisation.Should().Be("charity-commission");
        meta.Source.Should().Be("GOV.UK Content API");
        meta.Base_Url.Should().NotBeNullOrWhiteSpace();
        meta.Documentation_Url.Should().NotBeNullOrWhiteSpace();
    }

    private record GetContentResponse(object Content, string Url, string Public_Updated_At, string Attribution, string Disclaimer, string? Content_Id);
    private record SourceMetadata(string Organisation, string Source, string Base_Url, string Documentation_Url);
}
