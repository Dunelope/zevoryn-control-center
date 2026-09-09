namespace Zevoryn.Control.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Enums;

[ApiController, Route("api/beta/campaigns")]
public sealed class BetaCampaignsController(IBetaCampaignService campaigns) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<BetaCampaignDto>> Get([FromQuery] Guid? productId, [FromQuery] BetaCampaignStatus? status, CancellationToken ct) => campaigns.GetAsync(productId, status, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<BetaCampaignDto>> GetById(Guid id, CancellationToken ct) => (await campaigns.GetByIdAsync(id, ct)) is { } campaign ? Ok(campaign) : NotFound();
    [HttpPost] public async Task<ActionResult<BetaCampaignDto>> Create(CreateBetaCampaignRequest request, CancellationToken ct) { var campaign = await campaigns.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { id = campaign.Id }, campaign); }
    [HttpPut("{id:guid}")] public Task<BetaCampaignDto> Update(Guid id, UpdateBetaCampaignRequest request, CancellationToken ct) => campaigns.UpdateAsync(id, request, ct);
    [HttpPost("{id:guid}/activate")] public async Task<IActionResult> Activate(Guid id, CancellationToken ct) { await campaigns.ActivateAsync(id, ct); return NoContent(); }
    [HttpPost("{id:guid}/pause")] public async Task<IActionResult> Pause(Guid id, CancellationToken ct) { await campaigns.PauseAsync(id, ct); return NoContent(); }
    [HttpPost("{id:guid}/close")] public async Task<IActionResult> Close(Guid id, CancellationToken ct) { await campaigns.CloseAsync(id, ct); return NoContent(); }
}
