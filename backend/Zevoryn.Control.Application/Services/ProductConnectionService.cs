namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;

public sealed class ProductConnectionService(IProductRepository products, IProductEnvironmentRepository environments, IProductConnectionRepository repository) : IProductConnectionService
{
    public async Task<IReadOnlyList<ProductConnectionDto>> GetByEnvironmentIdAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken)
    {
        await RequireEnvironmentAsync(productId, environmentId, cancellationToken);
        return (await repository.GetByEnvironmentIdAsync(environmentId, cancellationToken)).Select(Map).ToList();
    }

    public async Task<ProductConnectionDto> CreateAsync(Guid productId, Guid environmentId, CreateProductConnectionRequest request, CancellationToken cancellationToken)
    {
        await RequireEnvironmentAsync(productId, environmentId, cancellationToken);
        if (await repository.ExistsByTypeAsync(environmentId, request.ConnectionType, null, cancellationToken)) throw new ConflictException("A connection of this type already exists for the environment.");
        var connection = ProductConnection.Create(environmentId, request.ConnectionType, request.SecretReference, request.AccessClientIdSecretReference, request.AccessClientSecretSecretReference);
        await repository.AddAsync(connection, cancellationToken); await repository.SaveChangesAsync(cancellationToken); return Map(connection);
    }

    public async Task<ProductConnectionDto> UpdateAsync(Guid productId, Guid environmentId, Guid connectionId, UpdateProductConnectionRequest request, CancellationToken cancellationToken)
    {
        await RequireEnvironmentAsync(productId, environmentId, cancellationToken);
        var connection = await repository.GetByIdForUpdateAsync(environmentId, connectionId, cancellationToken) ?? throw new ResourceNotFoundException($"Connection '{connectionId}' was not found.");
        if (await repository.ExistsByTypeAsync(environmentId, request.ConnectionType, connectionId, cancellationToken)) throw new ConflictException("A connection of this type already exists for the environment.");
        connection.Update(request.ConnectionType, request.SecretReference, request.AccessClientIdSecretReference, request.AccessClientSecretSecretReference, request.IsEnabled); await repository.SaveChangesAsync(cancellationToken); return Map(connection);
    }

    public async Task DeleteAsync(Guid productId, Guid environmentId, Guid connectionId, CancellationToken cancellationToken)
    {
        await RequireEnvironmentAsync(productId, environmentId, cancellationToken);
        var connection = await repository.GetByIdForUpdateAsync(environmentId, connectionId, cancellationToken) ?? throw new ResourceNotFoundException($"Connection '{connectionId}' was not found.");
        await repository.DeleteAsync(connection, cancellationToken); await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task RequireEnvironmentAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken)
    {
        if (await products.GetByIdAsync(productId, cancellationToken) is null) throw new ResourceNotFoundException($"Product '{productId}' was not found.");
        if (await environments.GetByIdAsync(productId, environmentId, cancellationToken) is null) throw new ResourceNotFoundException($"Environment '{environmentId}' was not found for product '{productId}'.");
    }

    private static ProductConnectionDto Map(ProductConnection c) => new(c.Id, c.ProductEnvironmentId, c.ConnectionType, c.SecretReference, c.AccessClientIdSecretReference, c.AccessClientSecretSecretReference, c.IsEnabled, c.LastSuccessfulConnectionAtUtc, c.LastFailureAtUtc, c.LastError, c.CreatedAtUtc, c.UpdatedAtUtc);
}
