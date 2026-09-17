namespace Zevoryn.Control.Infrastructure.Integrations;

using Microsoft.Extensions.Configuration;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Enums;

public sealed class CleanersFlowClientResolver(
    IHttpClientFactory clients,
    IProductRepository products,
    IProductEnvironmentRepository environments,
    IProductConnectionRepository connections,
    ISecretProvider secrets,
    IConfiguration configuration) : ICleanersFlowClientResolver
{
    public async Task<HttpClient> CreateDefaultAsync(CancellationToken ct)
    {
        var product = (await products.GetAllAsync(ct)).SingleOrDefault(x => string.Equals(x.Slug, "cleanersflow", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("CleanersFlow is not configured.");
        foreach (var environment in await environments.GetByProductIdAsync(product.Id, ct))
        {
            if ((await connections.GetByEnvironmentIdAsync(environment.Id, ct)).Any(x => x.ConnectionType == ConnectionType.InternalApi && x.IsEnabled))
                return await CreateAsync(product.Id, environment.Id, ct);
        }
        throw new InvalidOperationException("No enabled CleanersFlow InternalApi connection is configured.");
    }

    public async Task<HttpClient> CreateAsync(Guid productId, Guid environmentId, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(productId, ct) ?? throw new InvalidOperationException("Configured product was not found.");
        if (!string.Equals(product.Slug, "cleanersflow", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The CleanersFlow provider requires the cleanersflow product.");
        var environment = await environments.GetByIdAsync(productId, environmentId, ct) ?? throw new InvalidOperationException("The target environment does not belong to the product.");
        var connection = (await connections.GetByEnvironmentIdAsync(environment.Id, ct)).FirstOrDefault(x => x.ConnectionType == ConnectionType.InternalApi && x.IsEnabled)
            ?? throw new InvalidOperationException("No enabled CleanersFlow InternalApi connection is configured.");
        var hasAccessId = !string.IsNullOrWhiteSpace(connection.AccessClientIdSecretReference);
        var hasAccessSecret = !string.IsNullOrWhiteSpace(connection.AccessClientSecretSecretReference);
        if (hasAccessId != hasAccessSecret) throw new InvalidOperationException("Cloudflare Access configuration is incomplete.");
        if (!Uri.TryCreate(environment.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https")) throw new InvalidOperationException("The configured CleanersFlow base URL is invalid.");
        if (string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase) && baseUri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Production CleanersFlow connections must use HTTPS.");
        var controlSecret = await secrets.GetSecretAsync(connection.SecretReference, ct);
        if (string.IsNullOrWhiteSpace(controlSecret)) throw new InvalidOperationException("The CleanersFlow integration secret is unavailable.");
        var client = clients.CreateClient("cleanersflow-control");
        client.BaseAddress = new Uri(baseUri, baseUri.AbsoluteUri.EndsWith('/') ? baseUri.AbsoluteUri : baseUri.AbsoluteUri + "/");
        client.DefaultRequestHeaders.Remove("X-Zevoryn-Control-Key");
        client.DefaultRequestHeaders.Add("X-Zevoryn-Control-Key", controlSecret);
        client.DefaultRequestHeaders.Remove("CF-Access-Client-Id");
        client.DefaultRequestHeaders.Remove("CF-Access-Client-Secret");
        if (hasAccessId)
        {
            var accessId = await secrets.GetSecretAsync(connection.AccessClientIdSecretReference!, ct);
            var accessSecret = await secrets.GetSecretAsync(connection.AccessClientSecretSecretReference!, ct);
            if (string.IsNullOrWhiteSpace(accessId) || string.IsNullOrWhiteSpace(accessSecret)) throw new InvalidOperationException("Cloudflare Access credentials are unavailable.");
            client.DefaultRequestHeaders.Add("CF-Access-Client-Id", accessId);
            client.DefaultRequestHeaders.Add("CF-Access-Client-Secret", accessSecret);
        }
        return client;
    }
}
