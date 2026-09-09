namespace Zevoryn.Control.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Infrastructure.Persistence;
using Zevoryn.Control.Infrastructure.Secrets;
using Zevoryn.Control.Infrastructure.Health;
using Zevoryn.Control.Infrastructure.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ControlDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("ControlDatabase")));
        services.AddScoped<IProductRepository, ProductRepository>(); services.AddScoped<IProductEnvironmentRepository, ProductEnvironmentRepository>(); services.AddScoped<IProductConnectionRepository, ProductConnectionRepository>(); services.AddScoped<ISaaSEventRepository, SaaSEventRepository>(); services.AddScoped<IBetaCampaignRepository, BetaCampaignRepository>(); services.AddScoped<IBetaInvitationRepository, BetaInvitationRepository>();
        services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();
        services.AddHttpClient("cleanersflow-control", client => client.Timeout = TimeSpan.FromSeconds(10)).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<IBetaInvitationProvider, CleanersFlowBetaInvitationProvider>();
        services.AddScoped<IBetaInvitationProviderResolver, BetaInvitationProviderResolver>();
        services.AddHttpClient<IEnvironmentHealthChecker, HttpEnvironmentHealthChecker>(client => { client.Timeout = TimeSpan.FromSeconds(5); client.DefaultRequestHeaders.UserAgent.ParseAdd("Zevoryn-Control-Center-HealthCheck/1.0"); });
        return services;
    }
}
