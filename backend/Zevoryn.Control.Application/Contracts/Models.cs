namespace Zevoryn.Control.Application.Contracts;

using Zevoryn.Control.Domain.Enums;

public sealed record CreateProductRequest(string Name, string Slug, string? Description);
public sealed record UpdateProductRequest(string Name, string Slug, string? Description);
public sealed record ProductDto(Guid Id, string Name, string Slug, string? Description, ProductStatus Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateProductEnvironmentRequest(string Name, EnvironmentType EnvironmentType, string BaseUrl);
public sealed record UpdateProductEnvironmentRequest(string Name, EnvironmentType EnvironmentType, string BaseUrl);
public sealed record ProductEnvironmentDto(Guid Id, Guid ProductId, string Name, EnvironmentType EnvironmentType, string BaseUrl, EnvironmentStatus Status, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateProductConnectionRequest(ConnectionType ConnectionType, string SecretReference, bool IsEnabled = true);
public sealed record UpdateProductConnectionRequest(ConnectionType ConnectionType, string SecretReference, bool IsEnabled);
public sealed record ProductConnectionDto(Guid Id, Guid ProductEnvironmentId, ConnectionType ConnectionType, string SecretReference, bool IsEnabled, DateTime? LastSuccessfulConnectionAtUtc, DateTime? LastFailureAtUtc, string? LastError, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record EnvironmentHealthCheckResult(Guid EnvironmentId, EnvironmentStatus Status, int? HttpStatusCode, long? LatencyMs, string? Message, DateTime CheckedAtUtc);
public sealed record CreateBetaCampaignRequest(Guid ProductId, string Name, string? Description, int MaxInvitations, DateTime? StartsAtUtc, DateTime? EndsAtUtc, Guid EnvironmentId = default);
public sealed record UpdateBetaCampaignRequest(string Name, string? Description, int MaxInvitations, DateTime? StartsAtUtc, DateTime? EndsAtUtc, Guid? EnvironmentId = null);
public sealed record BetaCampaignDto(Guid Id, Guid ProductId, Guid? EnvironmentId, string Name, string? Description, BetaCampaignStatus Status, int MaxInvitations, int TotalInvitations, int PendingInvitations, int SentInvitations, int AcceptedInvitations, int RevokedInvitations, int FailedInvitations, int RemainingCapacity, DateTime? StartsAtUtc, DateTime? EndsAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateBetaInvitationRequest(string Email, DateTime? ExpiresAtUtc);
public sealed record BetaInvitationDto(Guid Id, Guid BetaCampaignId, string Email, BetaInvitationStatus Status, string? ExternalReference, string? ErrorCode, string? ErrorMessage, DateTime? InvitedAtUtc, DateTime? AcceptedAtUtc, DateTime? RevokedAtUtc, DateTime? ExpiresAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record CreateSaaSEventRequest(Guid ProductId, Guid? EnvironmentId, string Type, string? ExternalEntityId, string PayloadJson, DateTime OccurredAtUtc);
public sealed record SaaSEventDto(Guid Id, Guid ProductId, Guid? EnvironmentId, string Type, string? ExternalEntityId, string PayloadJson, DateTime OccurredAtUtc, DateTime ReceivedAtUtc);
