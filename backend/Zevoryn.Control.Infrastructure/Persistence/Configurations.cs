namespace Zevoryn.Control.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zevoryn.Control.Domain.Entities;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(200).IsRequired(); b.Property(x => x.Slug).HasMaxLength(100).IsRequired(); b.HasIndex(x => x.Slug).IsUnique(); b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Property(x => x.CreatedAtUtc).IsRequired(); b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.HasMany(x => x.Environments).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class ProductEnvironmentConfiguration : IEntityTypeConfiguration<ProductEnvironment>
{
    public void Configure(EntityTypeBuilder<ProductEnvironment> b)
    {
        b.ToTable("product_environments"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(100).IsRequired(); b.Property(x => x.BaseUrl).HasMaxLength(2048).IsRequired(); b.Property(x => x.EnvironmentType).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.HasIndex(x => new { x.ProductId, x.Name }).IsUnique();
    }
}
public sealed class SaaSEventConfiguration : IEntityTypeConfiguration<SaaSEvent>
{
    public void Configure(EntityTypeBuilder<SaaSEvent> b)
    {
        b.ToTable("saas_events"); b.HasKey(x => x.Id); b.Property(x => x.Type).HasMaxLength(200).IsRequired(); b.Property(x => x.ExternalEntityId).HasMaxLength(200); b.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired(); b.HasIndex(x => x.ProductId); b.HasIndex(x => x.Type); b.HasIndex(x => x.OccurredAtUtc); b.HasIndex(x => x.ReceivedAtUtc); b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); b.HasOne<ProductEnvironment>().WithMany().HasForeignKey(x => x.EnvironmentId).OnDelete(DeleteBehavior.SetNull);
    }
}
public sealed class ProductConnectionConfiguration : IEntityTypeConfiguration<ProductConnection>
{
    public void Configure(EntityTypeBuilder<ProductConnection> b)
    {
        b.ToTable("product_connections"); b.HasKey(x => x.Id); b.Property(x => x.ConnectionType).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Property(x => x.SecretReference).HasMaxLength(200).IsRequired(); b.Property(x => x.LastError).HasMaxLength(2000); b.Property(x => x.IsEnabled).IsRequired(); b.HasIndex(x => x.ProductEnvironmentId); b.HasIndex(x => new { x.ProductEnvironmentId, x.ConnectionType }).IsUnique(); b.HasOne(x => x.ProductEnvironment).WithMany().HasForeignKey(x => x.ProductEnvironmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class BetaCampaignConfiguration : IEntityTypeConfiguration<BetaCampaign>
{
    public void Configure(EntityTypeBuilder<BetaCampaign> b)
    {
        b.ToTable("beta_campaigns"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(200).IsRequired(); b.Property(x => x.Description).HasMaxLength(2000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Property(x => x.MaxInvitations).IsRequired(); b.HasIndex(x => x.ProductId); b.HasIndex(x => x.Status); b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); b.HasMany(x => x.Invitations).WithOne(x => x.BetaCampaign).HasForeignKey(x => x.BetaCampaignId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class BetaInvitationConfiguration : IEntityTypeConfiguration<BetaInvitation>
{
    public void Configure(EntityTypeBuilder<BetaInvitation> b)
    {
        b.ToTable("beta_invitations"); b.HasKey(x => x.Id); b.Property(x => x.Email).HasMaxLength(320).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); b.Property(x => x.ExternalReference).HasMaxLength(200); b.HasIndex(x => x.BetaCampaignId); b.HasIndex(x => x.Status); b.HasIndex(x => new { x.BetaCampaignId, x.Email }).IsUnique().HasFilter("\"Status\" NOT IN ('Revoked', 'Expired')");
    }
}
