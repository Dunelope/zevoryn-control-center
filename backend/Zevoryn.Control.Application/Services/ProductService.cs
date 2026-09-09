namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(Map).ToList();

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await repository.GetByIdAsync(id, cancellationToken)) is { } product ? Map(product) : null;

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsBySlugAsync(request.Slug, cancellationToken)) throw new ConflictException($"A product with slug '{request.Slug}' already exists.");
        var product = Product.Create(request.Name, request.Slug, request.Description);
        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    internal static ProductDto Map(Product p) => new(p.Id, p.Name, p.Slug, p.Description, p.Status, p.CreatedAtUtc, p.UpdatedAtUtc);
}

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
