namespace Zevoryn.Control.Tests;

using System.Net;
using System.Net.Http.Json;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Infrastructure.Integrations;

public sealed class CleanersFlowFeedbackReportTests
{
    [Fact]
    public async Task Lists_filtered_reports_and_maps_remote_fields()
    {
        var id = Guid.NewGuid();
        var handler = new Handler(HttpStatusCode.OK, $$"""[{"id":"{{id}}","companyId":"{{Guid.NewGuid()}}","companyName":"Acme","submittedByUserId":"{{Guid.NewGuid()}}","submitterName":"Alex","submitterEmail":"alex@example.com","type":"Bug","title":"Broken","description":"Details","currentPageRoute":"/jobs","status":"New","createdAtUtc":"2026-01-01T00:00:00Z","updatedAtUtc":"2026-01-01T00:00:00Z"}]""");
        var provider = new CleanersFlowFeedbackReportProvider(new StubResolver(handler));

        var result = await provider.GetAsync("new", "bug", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Acme", result[0].CompanyName);
        Assert.Equal("/jobs", result[0].CurrentPageRoute);
        Assert.Equal("api/internal/control/feedback-reports?status=New&type=Bug", handler.Request!.RequestUri!.PathAndQuery.TrimStart('/'));
    }

    [Fact]
    public async Task Updates_status_with_canonical_payload()
    {
        var id = Guid.NewGuid();
        var handler = new Handler(HttpStatusCode.OK, $$"""{"id":"{{id}}","companyId":"{{Guid.NewGuid()}}","companyName":"Acme","submittedByUserId":"{{Guid.NewGuid()}}","submitterName":"Alex","submitterEmail":"alex@example.com","type":"Other","title":"Note","description":"Details","currentPageRoute":"/","status":"Resolved","createdAtUtc":"2026-01-01T00:00:00Z","updatedAtUtc":"2026-01-01T00:00:00Z"}""");
        var provider = new CleanersFlowFeedbackReportProvider(new StubResolver(handler));

        var result = await provider.UpdateStatusAsync(id, "resolved", CancellationToken.None);

        Assert.Equal("Resolved", result.Status);
        Assert.Equal(HttpMethod.Patch, handler.Request!.Method);
        Assert.Equal($"api/internal/control/feedback-reports/{id}/status", handler.Request.RequestUri!.PathAndQuery.TrimStart('/'));
        Assert.Contains("\"status\":\"Resolved\"", await handler.Request.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task Rejects_invalid_filters_before_calling_cleanersflow()
    {
        var handler = new Handler(HttpStatusCode.OK, "[]");
        var provider = new CleanersFlowFeedbackReportProvider(new StubResolver(handler));

        await Assert.ThrowsAsync<ValidationException>(() => provider.GetAsync("Closed", null, CancellationToken.None));
        Assert.Null(handler.Request);
    }

    private sealed class StubResolver(Handler handler) : ICleanersFlowClientResolver
    {
        public Task<HttpClient> CreateAsync(Guid _, Guid __, CancellationToken ___) => Task.FromResult(new HttpClient(handler) { BaseAddress = new Uri("https://cleanersflow.test/") });
        public Task<HttpClient> CreateDefaultAsync(CancellationToken cancellationToken) => CreateAsync(Guid.Empty, Guid.Empty, cancellationToken);
    }

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken _)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(System.Text.Json.JsonSerializer.Deserialize<object>(body)) });
        }
    }
}
