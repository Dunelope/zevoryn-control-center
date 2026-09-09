namespace Zevoryn.Control.Application.Abstractions;

using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Domain.Enums;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}
public interface IProductEnvironmentService
{
    Task<IReadOnlyList<ProductEnvironmentDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<ProductEnvironmentDto?> GetByIdAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken);
    Task<ProductEnvironmentDto> CreateAsync(Guid productId, CreateProductEnvironmentRequest request, CancellationToken cancellationToken);
    Task<ProductEnvironmentDto> UpdateAsync(Guid productId, Guid environmentId, UpdateProductEnvironmentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken);
    Task<EnvironmentHealthCheckResult> CheckHealthAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken);
}
public interface IProductConnectionService
{
    Task<IReadOnlyList<ProductConnectionDto>> GetByEnvironmentIdAsync(Guid productId, Guid environmentId, CancellationToken cancellationToken);
    Task<ProductConnectionDto> CreateAsync(Guid productId, Guid environmentId, CreateProductConnectionRequest request, CancellationToken cancellationToken);
    Task<ProductConnectionDto> UpdateAsync(Guid productId, Guid environmentId, Guid connectionId, UpdateProductConnectionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid productId, Guid environmentId, Guid connectionId, CancellationToken cancellationToken);
}
public interface ISaaSEventService
{
    Task<IReadOnlyList<SaaSEventDto>> GetRecentAsync(CancellationToken cancellationToken);
    Task<SaaSEventDto> CreateAsync(CreateSaaSEventRequest request, CancellationToken cancellationToken);
}
public interface IControlEventPublisher
{
    Task PublishSaaSEventAsync(SaaSEventDto @event, CancellationToken cancellationToken);
}
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string secretReference, CancellationToken cancellationToken);
}
public interface IEnvironmentHealthChecker
{
    Task<EnvironmentHealthCheckResult> CheckAsync(Guid environmentId, string baseUrl, CancellationToken cancellationToken);
}
public sealed record BetaInvitationProviderResult(string? ExternalReference, BetaInvitationStatus Status, string? ErrorCode, string? ErrorMessage);
public interface IBetaInvitationProvider
{
    Task<BetaInvitationProviderResult> CreateInvitationAsync(Guid productId, string email, CancellationToken cancellationToken);
    Task<BetaInvitationProviderResult> RevokeInvitationAsync(Guid productId, string? externalReference, CancellationToken cancellationToken);
}
