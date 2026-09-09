namespace Zevoryn.Control.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;

[ApiController, Route("api/products/{productId:guid}/environments/{environmentId:guid}/connections")]
public sealed class ProductConnectionsController(IProductConnectionService connections) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ProductConnectionDto>> Get(Guid productId, Guid environmentId, CancellationToken ct) => connections.GetByEnvironmentIdAsync(productId, environmentId, ct);
    [HttpPost] public async Task<ActionResult<ProductConnectionDto>> Create(Guid productId, Guid environmentId, CreateProductConnectionRequest request, CancellationToken ct) => Ok(await connections.CreateAsync(productId, environmentId, request, ct));
    [HttpPut("{connectionId:guid}")] public Task<ProductConnectionDto> Update(Guid productId, Guid environmentId, Guid connectionId, UpdateProductConnectionRequest request, CancellationToken ct) => connections.UpdateAsync(productId, environmentId, connectionId, request, ct);
    [HttpDelete("{connectionId:guid}")] public async Task<IActionResult> Delete(Guid productId, Guid environmentId, Guid connectionId, CancellationToken ct) { await connections.DeleteAsync(productId, environmentId, connectionId, ct); return NoContent(); }
}
