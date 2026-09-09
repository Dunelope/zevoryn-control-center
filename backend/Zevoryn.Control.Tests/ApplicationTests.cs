namespace Zevoryn.Control.Tests;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Entities;

public sealed class ApplicationTests
{
    [Fact] public async Task Create_product_uses_repository() { var repo = new FakeProductRepository(); var service = new ProductService(repo); var result = await service.CreateAsync(new("Inkorya", "inkorya", null), default); Assert.Equal("inkorya", result.Slug); Assert.Single(repo.Items); }
    [Fact] public async Task Duplicate_slug_is_rejected() { var repo = new FakeProductRepository(); var service = new ProductService(repo); await service.CreateAsync(new("One", "one", null), default); await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new("Two", "one", null), default)); }
    [Fact] public async Task Missing_product_rejects_environment() { var products = new FakeProductRepository(); var environments = new FakeEnvironmentRepository(); var service = new ProductEnvironmentService(products, environments); await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.CreateAsync(Guid.NewGuid(), new("Dev", Zevoryn.Control.Domain.Enums.EnvironmentType.Development, "https://localhost"), default)); }
    [Fact] public async Task Create_event_persists_and_publishes() { var products = new FakeProductRepository(); await products.AddAsync(Product.Create("One", "one"), default); var publisher = new FakePublisher(); var service = new SaaSEventService(products, new FakeEnvironmentRepository(), new FakeEventRepository(), publisher); var result = await service.CreateAsync(new(products.Items[0].Id, null, "ServiceHealthy", null, "{}", DateTime.UtcNow), default); Assert.Equal(result.Id, publisher.Event!.Id); }

    private sealed class FakeProductRepository : IProductRepository { public List<Product> Items { get; } = []; public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken _) => Task.FromResult<IReadOnlyList<Product>>(Items); public Task<Product?> GetByIdAsync(Guid id, CancellationToken _) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id)); public Task<bool> ExistsBySlugAsync(string slug, CancellationToken _) => Task.FromResult(Items.Any(x => x.Slug == slug.Trim().ToLowerInvariant())); public Task AddAsync(Product p, CancellationToken _) { Items.Add(p); return Task.CompletedTask; } public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask; }
    private sealed class FakeEnvironmentRepository : IProductEnvironmentRepository { public List<ProductEnvironment> Items { get; } = []; public Task<IReadOnlyList<ProductEnvironment>> GetByProductIdAsync(Guid id, CancellationToken _) => Task.FromResult<IReadOnlyList<ProductEnvironment>>(Items.Where(x => x.ProductId == id).ToList()); public Task<bool> ExistsByNameAsync(Guid id, string name, CancellationToken _) => Task.FromResult(Items.Any(x => x.ProductId == id && x.Name == name)); public Task AddAsync(ProductEnvironment e, CancellationToken _) { Items.Add(e); return Task.CompletedTask; } public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask; }
    private sealed class FakeEventRepository : ISaaSEventRepository { public List<SaaSEvent> Items { get; } = []; public Task<IReadOnlyList<SaaSEvent>> GetRecentAsync(CancellationToken _) => Task.FromResult<IReadOnlyList<SaaSEvent>>(Items); public Task AddAsync(SaaSEvent e, CancellationToken _) { Items.Add(e); return Task.CompletedTask; } public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask; }
    private sealed class FakePublisher : IControlEventPublisher { public SaaSEventDto? Event { get; private set; } public Task PublishSaaSEventAsync(SaaSEventDto e, CancellationToken _) { Event = e; return Task.CompletedTask; } }
}
