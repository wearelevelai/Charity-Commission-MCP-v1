extern alias Server;
using System.Net.Http.Json;
using CCEW.Mcp.Server.Upstream;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

// Console harness that spins up a WireMock server to simulate GOV.UK API,
// hosts our ASP.NET app in-memory with that base address, calls the endpoint,
// and prints the full JSON (content included)

var mock = WireMockServer.Start();

// Prepare a realistic-ish GOV.UK content document
var contentDoc = new
{
    base_path = "/guidance/charity-annual-returns",
    public_updated_at = "2024-10-10T12:00:00Z",
    content_id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    title = "Charity annual returns",
    document_type = "guidance",
    details = new
    {
        body = new[]
        {
            new { content_type = "text/govspeak", content = "Annual returns guidance body..." }
        }
    }
};

// Wire mocks
mock.Given(Request.Create().WithPath("/api/content/guidance/charity-annual-returns").UsingGet())
    .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(contentDoc));

// Mock search by content_id returning link to the guidance path
var contentId = (string)contentDoc.GetType().GetProperty("content_id")!.GetValue(contentDoc)!;
var searchResponse = new
{
    total = 1,
    results = new object[]
    {
        new { title = contentDoc.GetType().GetProperty("title")!.GetValue(contentDoc), link = "/guidance/charity-annual-returns", description = "desc", public_timestamp = "2024-10-10T12:00:00Z", content_id = contentId }
    }
};
mock.Given(Request.Create().WithPath("/api/search.json").UsingGet())
    .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(searchResponse));

// Host app with overridden IGovUkClient HttpClient base
var factory = new MockFactory(mock);

using var client = factory.CreateClient();

var resp = await client.PostAsJsonAsync("/tools/get_content_by_path", new
{
    path = "/guidance/charity-annual-returns"
});
resp.EnsureSuccessStatusCode();

var json = await resp.Content.ReadAsStringAsync();
Console.WriteLine("--- get_content_by_path ---\n" + json + "\n");

// Now call get_content_by_id and print the full response
var byIdResp = await client.PostAsJsonAsync("/tools/get_content_by_id", new { content_id = contentId });
byIdResp.EnsureSuccessStatusCode();
var byIdJson = await byIdResp.Content.ReadAsStringAsync();
Console.WriteLine("--- get_content_by_id ---\n" + byIdJson + "\n");

mock.Stop();
mock.Dispose();

class MockFactory : WebApplicationFactory<Server::Program>
{
    private readonly WireMockServer _server;
    public MockFactory(WireMockServer server) => _server = server;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure content root points at our web project directory
        var cwd = Directory.GetCurrentDirectory();
        var serverProjectPath = Path.GetFullPath(Path.Combine(cwd, "src/CCEW.Mcp.Server"));
        builder.UseSetting(WebHostDefaults.ContentRootKey, serverProjectPath);
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<IGovUkClient, GovUkClient>(client =>
            {
                client.BaseAddress = new Uri(_server.Url!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
        });
    }
}