namespace Zevoryn.Control.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Entities;

public sealed class ProductRepository(ControlDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct) => await db.Products.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) => db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct) => db.Products.AnyAsync(x => x.Slug == slug.Trim().ToLower(), ct);
    public Task AddAsync(Product product, CancellationToken ct) => db.Products.AddAsync(product, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
public sealed class ProductEnvironmentRepository(ControlDbContext db) : IProductEnvironmentRepository
{
    public async Task<IReadOnlyList<ProductEnvironment>> GetByProductIdAsync(Guid id, CancellationToken ct) => await db.ProductEnvironments.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.Name).ToListAsync(ct);
    public Task<bool> ExistsByNameAsync(Guid id, string name, CancellationToken ct) => db.ProductEnvironments.AnyAsync(x => x.ProductId == id && x.Name == name.Trim(), ct);
    public Task AddAsync(ProductEnvironment environment, CancellationToken ct) => db.ProductEnvironments.AddAsync(environment, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
public sealed class SaaSEventRepository(ControlDbContext db) : ISaaSEventRepository
{
    public async Task<IReadOnlyList<SaaSEvent>> GetRecentAsync(CancellationToken ct) => await db.SaaSEvents.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(100).ToListAsync(ct);
    public Task AddAsync(SaaSEvent @event, CancellationToken ct) => db.SaaSEvents.AddAsync(@event, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
