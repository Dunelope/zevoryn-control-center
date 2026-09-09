namespace Zevoryn.Control.Domain.Entities;

using Zevoryn.Control.Domain.Enums;

public sealed class Product
{
    private Product() { }

    private Product(Guid id, string name, string slug, string? description, DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Description = description;
        Status = ProductStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProductStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public ICollection<ProductEnvironment> Environments { get; private set; } = new List<ProductEnvironment>();

    public static Product Create(string name, string slug, string? description = null, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Product slug cannot be empty.", nameof(slug));
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedSlug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Product slug must contain lowercase letters, numbers, and single hyphens.", nameof(slug));

        return new Product(Guid.NewGuid(), name.Trim(), normalizedSlug, description?.Trim(), nowUtc ?? DateTime.UtcNow);
    }

    public void Update(string name, string slug, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name cannot be empty.", nameof(name));
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedSlug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Product slug is invalid.", nameof(slug));
        Name = name.Trim(); Slug = normalizedSlug; Description = description?.Trim(); UpdatedAtUtc = DateTime.UtcNow;
    }
}
