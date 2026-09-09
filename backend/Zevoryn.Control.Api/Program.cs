using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Zevoryn.Control.Api.Hubs;
using Zevoryn.Control.Api.Publishing;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Infrastructure;
using Zevoryn.Control.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ControlExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres");
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("local", policy => policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"]).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IProductService, ProductService>(); builder.Services.AddScoped<IProductEnvironmentService, ProductEnvironmentService>(); builder.Services.AddScoped<ISaaSEventService, SaaSEventService>(); builder.Services.AddScoped<IControlEventPublisher, SignalRControlEventPublisher>();
var app = builder.Build();
if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ControlDbContext>().Database.MigrateAsync();
}
app.UseExceptionHandler(); app.UseCors("local"); app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false }); app.MapHealthChecks("/health/ready");
app.MapControllers(); app.MapHub<ControlEventsHub>("/hubs/control-events");
app.Run();

public partial class Program { }

public sealed class ControlExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ControlExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch { ResourceNotFoundException => (404, "Resource not found"), ConflictException => (409, "Conflict"), ValidationException or ArgumentException => (400, "Validation failed"), _ => (500, "An unexpected error occurred") };
        if (status == 500) logger.LogError(exception, "Unhandled request exception"); else logger.LogInformation(exception, "Request rejected with status {Status}", status);
        httpContext.Response.StatusCode = status; return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, Exception = exception, ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = status, Title = title, Detail = status == 500 ? null : exception.Message } });
    }
}

public sealed class PostgresHealthCheck(ControlDbContext db) : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
    }
}
