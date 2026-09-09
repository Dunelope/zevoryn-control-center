namespace Zevoryn.Control.Domain.Entities;

using Zevoryn.Control.Domain.Enums;

public sealed class ProductEnvironment
{
    private ProductEnvironment() { }

    private ProductEnvironment(Guid id, Guid productId, string name, EnvironmentType environmentType, Uri baseUrl, DateTime nowUtc)
    {
        Id = id; ProductId = productId; Name = name; EnvironmentType = environmentType; BaseUrl = baseUrl.ToString();
        Status = EnvironmentStatus.Unknown; CreatedAtUtc = nowUtc; UpdatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; private set; }
    public string BaseUrl { get; private set; } = string.Empty;
    public EnvironmentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Product Product { get; private set; } = null!;

    public static ProductEnvironment Create(Guid productId, string name, EnvironmentType type, string baseUrl, DateTime? nowUtc = null)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Environment must belong to a product.", nameof(productId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Environment name cannot be empty.", nameof(name));
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("BaseUrl must be a valid absolute HTTP or HTTPS URL.", nameof(baseUrl));
        return new ProductEnvironment(Guid.NewGuid(), productId, name.Trim(), type, uri, nowUtc ?? DateTime.UtcNow);
    }
}
