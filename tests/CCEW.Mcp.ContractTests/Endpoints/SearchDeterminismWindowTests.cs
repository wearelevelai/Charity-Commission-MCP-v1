using System.Net.Http.Json;
using CCEW.Mcp.ContractTests.TestUtils;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace CCEW.Mcp.ContractTests.Endpoints;

public class SearchDeterminismWindowTests : IClassFixture<MockedFactory>
{
    private readonly MockedFactory _factory;

    public SearchDeterminismWindowTests(MockedFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Repeated_Search_Within_Window_Should_Return_Identical_Ordering()
    {
        // Arrange a fixed upstream response
        _factory.Server.Reset();
        var apiResponse = new
        {
            total = 3,
            results = new object[]
            {
                new { title = "B", link = "/b", description = "b", public_timestamp = "2024-01-01T00:00:00Z", content_id = "2" },
                new { title = "C", link = "/c", description = "c", public_timestamp = "2023-01-01T00:00:00Z", content_id = "3" },
                new { title = "A", link = "/a", description = "a", public_timestamp = "2025-01-01T00:00:00Z", content_id = "1" }
            }
        };
        _factory.Server.Given(Request.Create().WithPath("/api/search.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(apiResponse));

        using var client = _factory.CreateClient();
        var payload = new { query = "charity governance", page = 1, pageSize = 20 };

        // Act
        var r1 = await client.PostAsJsonAsync("/tools/search_guidance", payload);
        var r2 = await client.PostAsJsonAsync("/tools/search_guidance", payload);

        // Assert
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();
        var a = await r1.Content.ReadFromJsonAsync<SearchOut>();
        var b = await r2.Content.ReadFromJsonAsync<SearchOut>();
        a!.Results.Should().BeEquivalentTo(b!.Results);
    }

    private record SearchOut(Result[] Results, int Page, int PageSize, int Total);
    private record Result(string Title, string Url, string? Summary, string? Public_Updated_At, string? Content_Id);
}
