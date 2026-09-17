namespace Zevoryn.Control.Infrastructure.Integrations;

using System.Net.Http.Json;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;

public sealed class CleanersFlowFeedbackReportProvider(ICleanersFlowClientResolver resolver) : ICleanersFlowFeedbackReportProvider
{
    private static readonly string[] ValidStatuses = ["New", "InProgress", "Resolved"];
    private static readonly string[] ValidTypes = ["Bug", "Suggestion", "Other"];

    public async Task<IReadOnlyList<FeedbackReportDto>> GetAsync(string? status, string? type, CancellationToken ct)
    {
        status = Canonicalize(status, ValidStatuses, "status"); type = Canonicalize(type, ValidTypes, "type");
        var query = new List<string>(); if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}"); if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");
        using var response = await (await resolver.CreateDefaultAsync(ct)).GetAsync($"api/internal/control/feedback-reports{(query.Count == 0 ? "" : "?" + string.Join('&', query))}", ct);
        EnsureSuccess(response, "list feedback reports");
        return (await response.Content.ReadFromJsonAsync<List<RemoteFeedbackReport>>(cancellationToken: ct) ?? []).Select(Map).ToList();
    }

    public async Task<FeedbackReportDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var response = await (await resolver.CreateDefaultAsync(ct)).GetAsync($"api/internal/control/feedback-reports/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        EnsureSuccess(response, "get feedback report");
        return Map(await response.Content.ReadFromJsonAsync<RemoteFeedbackReport>(cancellationToken: ct) ?? throw new InvalidOperationException("The provider returned an empty response."));
    }

    public async Task<FeedbackReportDto> UpdateStatusAsync(Guid id, string status, CancellationToken ct)
    {
        status = Canonicalize(status, ValidStatuses, "status") ?? throw new ValidationException("A feedback report status is required.");
        using var response = await (await resolver.CreateDefaultAsync(ct)).PatchAsJsonAsync($"api/internal/control/feedback-reports/{id}/status", new { status }, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) throw new ResourceNotFoundException($"Feedback report '{id}' was not found.");
        EnsureSuccess(response, "update feedback report status");
        return Map(await response.Content.ReadFromJsonAsync<RemoteFeedbackReport>(cancellationToken: ct) ?? throw new InvalidOperationException("The provider returned an empty response."));
    }

    private static string? Canonicalize(string? value, string[] valid, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var canonical = valid.FirstOrDefault(item => string.Equals(item, value.Trim(), StringComparison.OrdinalIgnoreCase));
        return canonical ?? throw new ValidationException($"Invalid feedback report {name}.");
    }
    private static void EnsureSuccess(HttpResponseMessage response, string operation) { if (!response.IsSuccessStatusCode) throw new ConflictException($"CleanersFlow could not {operation}."); }
    private static FeedbackReportDto Map(RemoteFeedbackReport r) => new(r.Id, r.CompanyId, r.CompanyName, r.SubmittedByUserId, r.SubmitterName, r.SubmitterEmail, r.Type, r.Title, r.Description, r.CurrentPageRoute, r.Status, r.CreatedAtUtc, r.UpdatedAtUtc);
    private sealed record RemoteFeedbackReport(Guid Id, Guid CompanyId, string CompanyName, Guid SubmittedByUserId, string SubmitterName, string SubmitterEmail, string Type, string Title, string Description, string CurrentPageRoute, string Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
}
