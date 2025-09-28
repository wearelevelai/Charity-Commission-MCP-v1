using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class GetContentByIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetContentByIdTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Should_echo_content_id_and_provide_provenance()
    {
        using var client = _factory.CreateClient();
        var input = new { content_id = "00000000-0000-0000-0000-000000000000" };
        var rsp = await client.PostAsJsonAsync("/tools/get_content_by_id", input);
        rsp.EnsureSuccessStatusCode();
        var body = await rsp.Content.ReadFromJsonAsync<GetByIdResponse>();
        body.Should().NotBeNull();
        body!.Content_Id.Should().NotBeNullOrWhiteSpace();
        body.Content_Id.Should().Be(input.content_id);
        body.Attribution.Should().NotBeNullOrWhiteSpace();
    }

    private record GetByIdResponse(object Content, string Url, string Public_Updated_At, string Attribution, string Content_Id);
}
