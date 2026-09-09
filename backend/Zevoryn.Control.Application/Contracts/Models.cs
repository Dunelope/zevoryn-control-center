namespace Zevoryn.Control.Application.Contracts;

using Zevoryn.Control.Domain.Enums;

public sealed record CreateProductRequest(string Name, string Slug, string? Description);
public sealed record ProductDto(Guid Id, string Name, string Slug, string? Description, ProductStatus Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateProductEnvironmentRequest(string Name, EnvironmentType EnvironmentType, string BaseUrl);
public sealed record ProductEnvironmentDto(Guid Id, Guid ProductId, string Name, EnvironmentType EnvironmentType, string BaseUrl, EnvironmentStatus Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateSaaSEventRequest(Guid ProductId, Guid? EnvironmentId, string Type, string? ExternalEntityId, string PayloadJson, DateTime OccurredAtUtc);
public sealed record SaaSEventDto(Guid Id, Guid ProductId, Guid? EnvironmentId, string Type, string? ExternalEntityId, string PayloadJson, DateTime OccurredAtUtc, DateTime ReceivedAtUtc);
