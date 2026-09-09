namespace Zevoryn.Control.Domain.Entities;

public sealed class SaaSEvent
{
    private SaaSEvent() { }

    private SaaSEvent(Guid id, Guid productId, Guid? environmentId, string type, string? externalEntityId, string payloadJson, DateTime occurredAtUtc, DateTime receivedAtUtc)
    {
        Id = id; ProductId = productId; EnvironmentId = environmentId; Type = type; ExternalEntityId = externalEntityId;
        PayloadJson = payloadJson; OccurredAtUtc = occurredAtUtc; ReceivedAtUtc = receivedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? EnvironmentId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? ExternalEntityId { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    public static SaaSEvent Create(Guid productId, Guid? environmentId, string type, string? externalEntityId, string payloadJson, DateTime occurredAtUtc, DateTime? receivedAtUtc = null)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Event must belong to a product.", nameof(productId));
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Event type cannot be empty.", nameof(type));
        if (string.IsNullOrWhiteSpace(payloadJson)) throw new ArgumentException("PayloadJson cannot be empty.", nameof(payloadJson));
        return new SaaSEvent(Guid.NewGuid(), productId, environmentId, type.Trim(), externalEntityId?.Trim(), payloadJson, EnsureUtc(occurredAtUtc), EnsureUtc(receivedAtUtc ?? DateTime.UtcNow));
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
