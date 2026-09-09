namespace Zevoryn.Control.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;

[ApiController, Route("api/products")]
public sealed class ProductsController(IProductService products, IProductEnvironmentService environments) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ProductDto>> Get(CancellationToken ct) => products.GetAllAsync(ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct) => (await products.GetByIdAsync(id, ct)) is { } product ? Ok(product) : NotFound();
    [HttpPost] public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct) { var product = await products.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { id = product.Id }, product); }
    [HttpPut("{id:guid}")] public Task<ProductDto> Update(Guid id, UpdateProductRequest request, CancellationToken ct) => products.UpdateAsync(id, request, ct);
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await products.DeactivateAsync(id, ct); return NoContent(); }
    [HttpGet("{productId:guid}/environments")] public Task<IReadOnlyList<ProductEnvironmentDto>> GetEnvironments(Guid productId, CancellationToken ct) => environments.GetByProductIdAsync(productId, ct);
    [HttpPost("{productId:guid}/environments")] public async Task<ActionResult<ProductEnvironmentDto>> CreateEnvironment(Guid productId, CreateProductEnvironmentRequest request, CancellationToken ct) => Ok(await environments.CreateAsync(productId, request, ct));
    [HttpGet("{productId:guid}/environments/{environmentId:guid}")] public async Task<ActionResult<ProductEnvironmentDto>> GetEnvironment(Guid productId, Guid environmentId, CancellationToken ct) => (await environments.GetByIdAsync(productId, environmentId, ct)) is { } environment ? Ok(environment) : NotFound();
    [HttpPut("{productId:guid}/environments/{environmentId:guid}")] public Task<ProductEnvironmentDto> UpdateEnvironment(Guid productId, Guid environmentId, UpdateProductEnvironmentRequest request, CancellationToken ct) => environments.UpdateAsync(productId, environmentId, request, ct);
    [HttpDelete("{productId:guid}/environments/{environmentId:guid}")] public async Task<IActionResult> DeleteEnvironment(Guid productId, Guid environmentId, CancellationToken ct) { await environments.DeleteAsync(productId, environmentId, ct); return NoContent(); }
    [HttpPost("{productId:guid}/environments/{environmentId:guid}/health-check")] public Task<EnvironmentHealthCheckResult> CheckHealth(Guid productId, Guid environmentId, CancellationToken ct) => environments.CheckHealthAsync(productId, environmentId, ct);
}
