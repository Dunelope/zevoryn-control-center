namespace Zevoryn.Control.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;

[ApiController, Route("api/events")]
public sealed class EventsController(ISaaSEventService events) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<SaaSEventDto>> Get(CancellationToken ct) => events.GetRecentAsync(ct);
    [HttpPost] public async Task<ActionResult<SaaSEventDto>> Create(CreateSaaSEventRequest request, CancellationToken ct) => Ok(await events.CreateAsync(request, ct));
}
