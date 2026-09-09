namespace Zevoryn.Control.Infrastructure.Health;

using System.Diagnostics;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Domain.Enums;

public sealed class HttpEnvironmentHealthChecker(HttpClient client) : IEnvironmentHealthChecker
{
    public async Task<EnvironmentHealthCheckResult> CheckAsync(Guid environmentId, string baseUrl, CancellationToken cancellationToken)
    {
        var checkedAt = DateTime.UtcNow; var stopwatch = Stopwatch.StartNew();
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            return new(environmentId, EnvironmentStatus.Offline, null, stopwatch.ElapsedMilliseconds, "Only HTTP and HTTPS URLs are supported.", checkedAt);
        try
        {
            using var response = await client.GetAsync(new Uri(baseUri, "/health"), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            stopwatch.Stop(); var status = (int)response.StatusCode is >= 200 and < 300 ? EnvironmentStatus.Healthy : EnvironmentStatus.Degraded;
            return new(environmentId, status, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode ? null : $"Health endpoint returned {(int)response.StatusCode}.", checkedAt);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(environmentId, EnvironmentStatus.Offline, null, stopwatch.ElapsedMilliseconds, "Health check timed out.", checkedAt); }
        catch (HttpRequestException exception) { return new(environmentId, EnvironmentStatus.Offline, null, stopwatch.ElapsedMilliseconds, exception.Message, checkedAt); }
    }
}
