namespace Zevoryn.Control.Tests;

using System.Net;
using System.Net.Http;
using Zevoryn.Control.Infrastructure.Health;

public sealed class HealthCheckerTests
{
    [Fact]
    public async Task Successful_health_response_returns_healthy_result()
    {
        using var client = new HttpClient(new StubHandler(HttpStatusCode.OK));
        var result = await new HttpEnvironmentHealthChecker(client).CheckAsync(Guid.NewGuid(), "https://example.test", default);
        Assert.Equal(Zevoryn.Control.Domain.Enums.EnvironmentStatus.Healthy, result.Status);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(result.LatencyMs);
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(status));
    }
}
