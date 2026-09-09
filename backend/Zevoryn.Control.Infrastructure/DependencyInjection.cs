namespace Zevoryn.Control.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ControlDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("ControlDatabase")));
        services.AddScoped<IProductRepository, ProductRepository>(); services.AddScoped<IProductEnvironmentRepository, ProductEnvironmentRepository>(); services.AddScoped<ISaaSEventRepository, SaaSEventRepository>();
        return services;
    }
}
