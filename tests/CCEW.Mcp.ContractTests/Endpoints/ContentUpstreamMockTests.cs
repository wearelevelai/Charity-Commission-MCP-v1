using System.Net;
using System.Net.Http.Json;
using CCEW.Mcp.ContractTests.TestUtils;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class ContentUpstreamMockTests : IClassFixture<MockedFactory>
{
    private readonly MockedFactory _factory;

    public ContentUpstreamMockTests(MockedFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetContentByPath_Maps404_To_NOT_FOUND_OR_REDIRECTED()
    {
        _factory.Server.Reset();
        _factory.Server.Given(Request.Create().WithPath("/api/content/missing/path").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.NotFound));

        using var client = _factory.CreateClient();
        var rsp = await client.PostAsJsonAsync("/tools/get_content_by_path", new { path = "/missing/path", options = new { strict_upstream_errors = true } });
        rsp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var err = await rsp.Content.ReadFromJsonAsync<ErrorOut>();
        err!.Code.Should().Be("NOT_FOUND_OR_REDIRECTED");
    }

    private record ErrorOut(string Code, string Error);
}
