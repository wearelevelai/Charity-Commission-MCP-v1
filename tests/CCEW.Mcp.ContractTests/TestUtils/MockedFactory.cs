using CCEW.Mcp.Server.Upstream;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;

namespace CCEW.Mcp.ContractTests.TestUtils;

public class MockedFactory : WebApplicationFactory<Program>, IDisposable
{
    public new WireMockServer Server { get; }

    public MockedFactory()
    {
        Server = WireMockServer.Start();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace IGovUkClient HttpClient base address to point to mock server
            services.AddHttpClient<IGovUkClient, GovUkClient>(client =>
            {
                client.BaseAddress = new Uri(Server.Url!);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Server.Stop();
            Server.Dispose();
        }
    }
}
