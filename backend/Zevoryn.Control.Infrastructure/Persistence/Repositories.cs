namespace Zevoryn.Control.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class ProductRepository(ControlDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct) => await db.Products.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) => db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken ct) => db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct) => db.Products.AnyAsync(x => x.Slug == slug.Trim().ToLowerInvariant(), ct);
    public Task<bool> ExistsBySlugAsync(string slug, Guid excludingId, CancellationToken ct) => db.Products.AnyAsync(x => x.Slug == slug.Trim().ToLowerInvariant() && x.Id != excludingId, ct);
    public Task AddAsync(Product product, CancellationToken ct) => db.Products.AddAsync(product, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
public sealed class ProductEnvironmentRepository(ControlDbContext db) : IProductEnvironmentRepository
{
    public async Task<IReadOnlyList<ProductEnvironment>> GetByProductIdAsync(Guid id, CancellationToken ct) => await db.ProductEnvironments.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.Name).ToListAsync(ct);
    public Task<ProductEnvironment?> GetByIdAsync(Guid productId, Guid environmentId, CancellationToken ct) => db.ProductEnvironments.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId && x.Id == environmentId, ct);
    public Task<ProductEnvironment?> GetByIdForUpdateAsync(Guid productId, Guid environmentId, CancellationToken ct) => db.ProductEnvironments.FirstOrDefaultAsync(x => x.ProductId == productId && x.Id == environmentId, ct);
    public Task<bool> ExistsByNameAsync(Guid id, string name, CancellationToken ct) => db.ProductEnvironments.AnyAsync(x => x.ProductId == id && x.Name == name.Trim(), ct);
    public Task<bool> ExistsByNameAsync(Guid id, string name, Guid excludingId, CancellationToken ct) => db.ProductEnvironments.AnyAsync(x => x.ProductId == id && x.Id != excludingId && x.Name == name.Trim(), ct);
    public Task AddAsync(ProductEnvironment environment, CancellationToken ct) => db.ProductEnvironments.AddAsync(environment, ct).AsTask();
    public Task DeleteAsync(ProductEnvironment environment, CancellationToken ct) { db.ProductEnvironments.Remove(environment); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
public sealed class SaaSEventRepository(ControlDbContext db) : ISaaSEventRepository
{
    public async Task<IReadOnlyList<SaaSEvent>> GetRecentAsync(CancellationToken ct) => await db.SaaSEvents.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(100).ToListAsync(ct);
    public Task AddAsync(SaaSEvent @event, CancellationToken ct) => db.SaaSEvents.AddAsync(@event, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
public sealed class ProductConnectionRepository(ControlDbContext db) : IProductConnectionRepository
{
    public async Task<IReadOnlyList<ProductConnection>> GetByEnvironmentIdAsync(Guid id, CancellationToken ct) => await db.ProductConnections.AsNoTracking().Where(x => x.ProductEnvironmentId == id).OrderBy(x => x.ConnectionType).ToListAsync(ct);
    public Task<ProductConnection?> GetByIdAsync(Guid environmentId, Guid connectionId, CancellationToken ct) => db.ProductConnections.AsNoTracking().FirstOrDefaultAsync(x => x.ProductEnvironmentId == environmentId && x.Id == connectionId, ct);
    public Task<ProductConnection?> GetByIdForUpdateAsync(Guid environmentId, Guid connectionId, CancellationToken ct) => db.ProductConnections.FirstOrDefaultAsync(x => x.ProductEnvironmentId == environmentId && x.Id == connectionId, ct);
    public Task<bool> ExistsByEnvironmentIdAsync(Guid environmentId, CancellationToken ct) => db.ProductConnections.AnyAsync(x => x.ProductEnvironmentId == environmentId, ct);
    public Task<bool> ExistsByTypeAsync(Guid environmentId, ConnectionType connectionType, Guid? excludingId, CancellationToken ct) => db.ProductConnections.AnyAsync(x => x.ProductEnvironmentId == environmentId && x.ConnectionType == connectionType && (excludingId == null || x.Id != excludingId), ct);
    public Task AddAsync(ProductConnection connection, CancellationToken ct) => db.ProductConnections.AddAsync(connection, ct).AsTask();
    public Task DeleteAsync(ProductConnection connection, CancellationToken ct) { db.ProductConnections.Remove(connection); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
