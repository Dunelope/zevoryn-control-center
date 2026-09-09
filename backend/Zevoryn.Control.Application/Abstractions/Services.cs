namespace Zevoryn.Control.Application.Abstractions;

using Zevoryn.Control.Application.Contracts;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
}
public interface IProductEnvironmentService
{
    Task<IReadOnlyList<ProductEnvironmentDto>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<ProductEnvironmentDto> CreateAsync(Guid productId, CreateProductEnvironmentRequest request, CancellationToken cancellationToken);
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
