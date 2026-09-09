namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;

public sealed class ProductEnvironmentService(IProductRepository products, IProductEnvironmentRepository repository) : IProductEnvironmentService
{
    public async Task<IReadOnlyList<ProductEnvironmentDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken) =>
        (await repository.GetByProductIdAsync(productId, cancellationToken)).Select(Map).ToList();

    public async Task<ProductEnvironmentDto> CreateAsync(Guid productId, CreateProductEnvironmentRequest request, CancellationToken cancellationToken)
    {
        if (await products.GetByIdAsync(productId, cancellationToken) is null) throw new ResourceNotFoundException($"Product '{productId}' was not found.");
        if (await repository.ExistsByNameAsync(productId, request.Name, cancellationToken)) throw new ConflictException($"An environment named '{request.Name}' already exists for this product.");
        var environment = ProductEnvironment.Create(productId, request.Name, request.EnvironmentType, request.BaseUrl);
        await repository.AddAsync(environment, cancellationToken); await repository.SaveChangesAsync(cancellationToken); return Map(environment);
    }

    private static ProductEnvironmentDto Map(ProductEnvironment e) => new(e.Id, e.ProductId, e.Name, e.EnvironmentType, e.BaseUrl, e.Status, e.CreatedAtUtc, e.UpdatedAtUtc);
}
public interface IProductEnvironmentRepository
{
    Task<IReadOnlyList<ProductEnvironment>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(Guid productId, string name, CancellationToken cancellationToken);
    Task AddAsync(ProductEnvironment environment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
