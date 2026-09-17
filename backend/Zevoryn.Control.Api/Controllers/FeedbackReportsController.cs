namespace Zevoryn.Control.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;

[ApiController, Route("api/feedback-reports")]
public sealed class FeedbackReportsController(ICleanersFlowFeedbackReportProvider provider) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<FeedbackReportDto>> Get([FromQuery] string? status, [FromQuery] string? type, CancellationToken ct) => provider.GetAsync(status, type, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<FeedbackReportDto>> GetById(Guid id, CancellationToken ct) => (await provider.GetByIdAsync(id, ct)) is { } report ? Ok(report) : NotFound();
    [HttpPatch("{id:guid}/status")] public Task<FeedbackReportDto> UpdateStatus(Guid id, UpdateFeedbackReportStatusRequest request, CancellationToken ct) => provider.UpdateStatusAsync(id, request.Status, ct);
}
