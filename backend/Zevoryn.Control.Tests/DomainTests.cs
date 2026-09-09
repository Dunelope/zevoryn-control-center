namespace Zevoryn.Control.Tests;

using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class DomainTests
{
    [Fact] public void Product_creation_normalizes_slug() { var p = Product.Create("CleanersFlow", " CleanersFlow "); Assert.Equal("cleanersflow", p.Slug); }
    [Fact] public void Product_rejects_empty_name() => Assert.Throws<ArgumentException>(() => Product.Create("", "product"));
    [Fact] public void Product_rejects_invalid_slug() => Assert.Throws<ArgumentException>(() => Product.Create("Product", "not valid"));
    [Fact] public void Environment_accepts_absolute_url() { var e = ProductEnvironment.Create(Guid.NewGuid(), "Production", EnvironmentType.Production, "https://example.com"); Assert.Equal("https://example.com/", e.BaseUrl); }
    [Fact] public void Environment_rejects_invalid_url() => Assert.Throws<ArgumentException>(() => ProductEnvironment.Create(Guid.NewGuid(), "Production", EnvironmentType.Production, "localhost"));
    [Fact] public void Event_accepts_valid_payload() { var e = SaaSEvent.Create(Guid.NewGuid(), null, "CustomerCreated", "c-1", "{}", DateTime.UtcNow); Assert.Equal("CustomerCreated", e.Type); }
    [Fact] public void Event_rejects_empty_type() => Assert.Throws<ArgumentException>(() => SaaSEvent.Create(Guid.NewGuid(), null, "", null, "{}", DateTime.UtcNow));
}
