namespace Zevoryn.Control.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Services;

[ApiController, Route("api/beta/campaigns/{campaignId:guid}/invitations")]
public sealed class BetaInvitationsController(IBetaInvitationService invitations) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<BetaInvitationDto>> Get(Guid campaignId, CancellationToken ct) => invitations.GetAsync(campaignId, ct);
    [HttpGet("{invitationId:guid}")] public async Task<ActionResult<BetaInvitationDto>> GetById(Guid campaignId, Guid invitationId, CancellationToken ct) => (await invitations.GetByIdAsync(campaignId, invitationId, ct)) is { } invitation ? Ok(invitation) : NotFound();
    [HttpPost] public async Task<ActionResult<BetaInvitationDto>> Create(Guid campaignId, CreateBetaInvitationRequest request, CancellationToken ct) => Ok(await invitations.CreateAsync(campaignId, request, ct));
    [HttpPost("{invitationId:guid}/retry")] public Task<BetaInvitationDto> Retry(Guid campaignId, Guid invitationId, CancellationToken ct) => invitations.RetryAsync(campaignId, invitationId, ct);
    [HttpPost("{invitationId:guid}/sync")] public Task<BetaInvitationDto> Sync(Guid campaignId, Guid invitationId, CancellationToken ct) => invitations.SyncAsync(campaignId, invitationId, ct);
    [HttpPost("{invitationId:guid}/revoke")] public async Task<IActionResult> Revoke(Guid campaignId, Guid invitationId, CancellationToken ct) { await invitations.RevokeAsync(campaignId, invitationId, ct); return NoContent(); }
}
