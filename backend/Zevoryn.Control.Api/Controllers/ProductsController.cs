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
    [HttpGet("{productId:guid}/environments")] public Task<IReadOnlyList<ProductEnvironmentDto>> GetEnvironments(Guid productId, CancellationToken ct) => environments.GetByProductIdAsync(productId, ct);
    [HttpPost("{productId:guid}/environments")] public async Task<ActionResult<ProductEnvironmentDto>> CreateEnvironment(Guid productId, CreateProductEnvironmentRequest request, CancellationToken ct) => Ok(await environments.CreateAsync(productId, request, ct));
}
