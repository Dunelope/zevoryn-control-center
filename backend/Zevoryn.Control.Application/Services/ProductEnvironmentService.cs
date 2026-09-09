namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class ProductEnvironmentService(IProductRepository products, IProductEnvironmentRepository repository, IProductConnectionRepository connections, IEnvironmentHealthChecker healthChecker, ISaaSEventService events) : IProductEnvironmentService
{
    public async Task<IReadOnlyList<ProductEnvironmentDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken) =>
        (await repository.GetByProductIdAsync(productId, cancellationToken)).Select(Map).ToList();

    public async Task<ProductEnvironmentDto?> GetByIdAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken) =>
        (await repository.GetByIdAsync(productId, environmentId, cancellationToken)) is { } environment ? Map(environment) : null;

    public async Task<ProductEnvironmentDto> CreateAsync(Guid productId, CreateProductEnvironmentRequest request, CancellationToken cancellationToken)
    {
        if (await products.GetByIdAsync(productId, cancellationToken) is null) throw new ResourceNotFoundException($"Product '{productId}' was not found.");
        if (await repository.ExistsByNameAsync(productId, request.Name, cancellationToken)) throw new ConflictException($"An environment named '{request.Name}' already exists for this product.");
        var environment = ProductEnvironment.Create(productId, request.Name, request.EnvironmentType, request.BaseUrl);
        await repository.AddAsync(environment, cancellationToken); await repository.SaveChangesAsync(cancellationToken); return Map(environment);
    }

    public async Task<ProductEnvironmentDto> UpdateAsync(Guid productId, Guid environmentId, UpdateProductEnvironmentRequest request, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdForUpdateAsync(productId, environmentId, cancellationToken) ?? throw new ResourceNotFoundException($"Environment '{environmentId}' was not found for product '{productId}'.");
        if (await repository.ExistsByNameAsync(productId, request.Name, environmentId, cancellationToken)) throw new ConflictException($"An environment named '{request.Name}' already exists for this product.");
        environment.Update(request.Name, request.EnvironmentType, request.BaseUrl);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(environment);
    }

    public async Task DeleteAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdForUpdateAsync(productId, environmentId, cancellationToken) ?? throw new ResourceNotFoundException($"Environment '{environmentId}' was not found for product '{productId}'.");
        if (await connections.ExistsByEnvironmentIdAsync(environmentId, cancellationToken)) throw new ConflictException("The environment cannot be deleted while connections exist.");
        await repository.DeleteAsync(environment, cancellationToken); await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<EnvironmentHealthCheckResult> CheckHealthAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdForUpdateAsync(productId, environmentId, cancellationToken) ?? throw new ResourceNotFoundException($"Environment '{environmentId}' was not found for product '{productId}'.");
        var previousStatus = environment.Status;
        var result = await healthChecker.CheckAsync(environment.Id, environment.BaseUrl, cancellationToken);
        environment.UpdateHealth(result.Status, result.CheckedAtUtc);
        await repository.SaveChangesAsync(cancellationToken);
        if (previousStatus != result.Status)
            await events.CreateAsync(new CreateSaaSEventRequest(environment.ProductId, environment.Id, "EnvironmentHealthChanged", environment.Id.ToString(), System.Text.Json.JsonSerializer.Serialize(result), result.CheckedAtUtc), cancellationToken);
        return result;
    }

    private static ProductEnvironmentDto Map(ProductEnvironment e) => new(e.Id, e.ProductId, e.Name, e.EnvironmentType, e.BaseUrl, e.Status, e.CreatedAtUtc, e.UpdatedAtUtc);
}
public interface IProductEnvironmentRepository
{
    Task<IReadOnlyList<ProductEnvironment>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<ProductEnvironment?> GetByIdAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken);
    Task<ProductEnvironment?> GetByIdForUpdateAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(Guid productId, string name, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(Guid productId, string name, Guid excludingId, CancellationToken cancellationToken);
    Task AddAsync(ProductEnvironment environment, CancellationToken cancellationToken);
    Task DeleteAsync(ProductEnvironment environment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IProductConnectionRepository
{
    Task<IReadOnlyList<ProductConnection>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<ProductConnection?> GetByIdAsync(Guid environmentId, Guid connectionId, CancellationToken cancellationToken);
    Task<ProductConnection?> GetByIdForUpdateAsync(Guid environmentId, Guid connectionId, CancellationToken cancellationToken);
    Task<bool> ExistsByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<bool> ExistsByTypeAsync(Guid environmentId, ConnectionType connectionType, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(ProductConnection connection, CancellationToken cancellationToken);
    Task DeleteAsync(ProductConnection connection, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
