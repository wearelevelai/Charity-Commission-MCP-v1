using System.Net;
using System.Net.Http.Json;
using CCEW.Mcp.ContractTests.TestUtils;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class SearchUpstreamMockTests : IClassFixture<MockedFactory>
{
    private readonly MockedFactory _factory;

    public SearchUpstreamMockTests(MockedFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_Should_MapResults_And_OrderByPublicTimestampDesc()
    {
        var apiResponse = new
        {
            total = 2,
            results = new object[]
            {
                new { title = "B", link = "/b", description = "b", public_timestamp = "2020-01-01T00:00:00Z", content_id = "2" },
                new { title = "A", link = "/a", description = "a", public_timestamp = "2021-01-01T00:00:00Z", content_id = "1" }
            }
        };

        _factory.Server.Given(Request.Create().WithPath("/api/search.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(apiResponse));

        using var client = _factory.CreateClient();
        var payload = new { query = "annual return", page = 1, pageSize = 20 };
        var rsp = await client.PostAsJsonAsync("/tools/search_guidance", payload);
        rsp.EnsureSuccessStatusCode();
        var json = await rsp.Content.ReadFromJsonAsync<SearchOut>();
        json.Should().NotBeNull();
        json!.Results.Should().HaveCount(2);
        json.Results[0].Title.Should().Be("A"); // 2021 first
        json.Results[0].Url.Should().Be("https://www.gov.uk/a");
        json.Results[1].Title.Should().Be("B");
        json.Page.Should().Be(1);
        json.PageSize.Should().Be(20);
        json.Total.Should().Be(2);
    }

    [Fact]
    public async Task Search_Should_Map_422_To_UPSTREAM_PARAMETER_ERROR()
    {
        _factory.Server.Reset();
        _factory.Server.Given(Request.Create().WithPath("/api/search.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.UnprocessableEntity).WithBody("{}"));

        using var client = _factory.CreateClient();
        var payload = new { query = "annual return", filters = new { public_timestamp_from = "not-a-date" } };
        var rsp = await client.PostAsJsonAsync("/tools/search_guidance", payload);
        rsp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await rsp.Content.ReadFromJsonAsync<ErrorOut>();
        err!.Code.Should().Be("UPSTREAM_PARAMETER_ERROR");
    }

    private record SearchOut(Result[] Results, int Page, int PageSize, int Total);
    private record Result(string Title, string Url, string? Summary, string? Public_Updated_At, string? Content_Id);
    private record ErrorOut(string Code, string Error);
}
