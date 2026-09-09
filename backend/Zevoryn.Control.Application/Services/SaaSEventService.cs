namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;

public sealed class SaaSEventService(IProductRepository products, IProductEnvironmentRepository environments, ISaaSEventRepository repository, IControlEventPublisher publisher) : ISaaSEventService
{
    public async Task<IReadOnlyList<SaaSEventDto>> GetRecentAsync(CancellationToken cancellationToken) =>
        (await repository.GetRecentAsync(cancellationToken)).Select(Map).ToList();

    public async Task<SaaSEventDto> CreateAsync(CreateSaaSEventRequest request, CancellationToken cancellationToken)
    {
        if (await products.GetByIdAsync(request.ProductId, cancellationToken) is null) throw new ResourceNotFoundException($"Product '{request.ProductId}' was not found.");
        if (request.EnvironmentId is { } environmentId && !(await environments.GetByProductIdAsync(request.ProductId, cancellationToken)).Any(e => e.Id == environmentId))
            throw new ValidationException("The environment does not belong to the supplied product.");
        try { System.Text.Json.JsonDocument.Parse(request.PayloadJson); } catch (System.Text.Json.JsonException) { throw new ValidationException("PayloadJson must be valid JSON."); }
        var @event = SaaSEvent.Create(request.ProductId, request.EnvironmentId, request.Type, request.ExternalEntityId, request.PayloadJson, request.OccurredAtUtc);
        await repository.AddAsync(@event, cancellationToken); await repository.SaveChangesAsync(cancellationToken);
        var dto = Map(@event); await publisher.PublishSaaSEventAsync(dto, cancellationToken); return dto;
    }
    private static SaaSEventDto Map(SaaSEvent e) => new(e.Id, e.ProductId, e.EnvironmentId, e.Type, e.ExternalEntityId, e.PayloadJson, e.OccurredAtUtc, e.ReceivedAtUtc);
}
public interface ISaaSEventRepository
{
    Task<IReadOnlyList<SaaSEvent>> GetRecentAsync(CancellationToken cancellationToken);
    Task AddAsync(SaaSEvent @event, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
