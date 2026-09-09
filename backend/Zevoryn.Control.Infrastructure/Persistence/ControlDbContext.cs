namespace Zevoryn.Control.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Zevoryn.Control.Domain.Entities;

public sealed class ControlDbContext(DbContextOptions<ControlDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductEnvironment> ProductEnvironments => Set<ProductEnvironment>();
    public DbSet<SaaSEvent> SaaSEvents => Set<SaaSEvent>();
    public DbSet<ProductConnection> ProductConnections => Set<ProductConnection>();
    public DbSet<BetaCampaign> BetaCampaigns => Set<BetaCampaign>();
    public DbSet<BetaInvitation> BetaInvitations => Set<BetaInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlDbContext).Assembly);
    }
}
