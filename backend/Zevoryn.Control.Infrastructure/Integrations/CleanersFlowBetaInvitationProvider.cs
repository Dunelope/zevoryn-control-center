namespace Zevoryn.Control.Infrastructure.Integrations;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Enums;

public sealed class CleanersFlowBetaInvitationProvider(IHttpClientFactory clients, IProductRepository products, IProductEnvironmentRepository environments, IProductConnectionRepository connections, ISecretProvider secrets, IConfiguration configuration) : IBetaInvitationProvider
{
    private async Task<HttpClient> ClientAsync(Guid productId, Guid environmentId, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(productId, ct) ?? throw new InvalidOperationException("Configured product was not found.");
        if (!string.Equals(product.Slug, "cleanersflow", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The CleanersFlow provider requires the cleanersflow product.");
        var environment = await environments.GetByIdAsync(productId, environmentId, ct) ?? throw new InvalidOperationException("The target environment does not belong to the product.");
        foreach (var connection in await connections.GetByEnvironmentIdAsync(environment.Id, ct))
            if (connection.ConnectionType == ConnectionType.InternalApi && connection.IsEnabled)
            {
                var hasAccessClientIdReference = !string.IsNullOrWhiteSpace(connection.AccessClientIdSecretReference);
                var hasAccessClientSecretReference = !string.IsNullOrWhiteSpace(connection.AccessClientSecretSecretReference);
                if (hasAccessClientIdReference != hasAccessClientSecretReference) throw new InvalidOperationException("Cloudflare Access configuration is incomplete.");
                if (!Uri.TryCreate(environment.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https")) throw new InvalidOperationException("The configured CleanersFlow base URL is invalid.");
                if (string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase) && !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Production CleanersFlow connections must use HTTPS.");
                var secret = await secrets.GetSecretAsync(connection.SecretReference, ct); if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("The CleanersFlow integration secret is unavailable.");
                var client = clients.CreateClient("cleanersflow-control"); client.BaseAddress = new Uri(baseUri, baseUri.AbsoluteUri.EndsWith('/') ? baseUri.AbsoluteUri : baseUri.AbsoluteUri + "/"); client.DefaultRequestHeaders.Remove("X-Zevoryn-Control-Key"); client.DefaultRequestHeaders.Add("X-Zevoryn-Control-Key", secret);
                if (hasAccessClientIdReference)
                {
                    var accessClientId = await secrets.GetSecretAsync(connection.AccessClientIdSecretReference!, ct);
                    var accessClientSecret = await secrets.GetSecretAsync(connection.AccessClientSecretSecretReference!, ct);
                    if (string.IsNullOrWhiteSpace(accessClientId) || string.IsNullOrWhiteSpace(accessClientSecret)) throw new InvalidOperationException("Cloudflare Access credentials are unavailable.");
                    client.DefaultRequestHeaders.Remove("CF-Access-Client-Id"); client.DefaultRequestHeaders.Remove("CF-Access-Client-Secret"); client.DefaultRequestHeaders.Add("CF-Access-Client-Id", accessClientId); client.DefaultRequestHeaders.Add("CF-Access-Client-Secret", accessClientSecret);
                }
                return client;
            }
        throw new InvalidOperationException("No enabled CleanersFlow InternalApi connection is configured.");
    }
    public async Task<BetaInvitationProviderResult> CreateInvitationAsync(BetaInvitationProviderRequest request, CancellationToken ct) => await SendAsync(request.ProductId, request.EnvironmentId, HttpMethod.Post, "api/internal/control/beta-invitations", new { email = request.Email, sourceReference = request.SourceReference }, ct);
    public async Task<BetaInvitationProviderResult> GetInvitationAsync(BetaInvitationProviderRequest request, CancellationToken ct) => await SendAsync(request.ProductId, request.EnvironmentId, HttpMethod.Get, $"api/internal/control/beta-invitations/{request.ExternalReference}", null, ct);
    public async Task<BetaInvitationProviderResult> RevokeInvitationAsync(BetaInvitationProviderRequest request, CancellationToken ct) => await SendAsync(request.ProductId, request.EnvironmentId, HttpMethod.Post, $"api/internal/control/beta-invitations/{request.ExternalReference}/revoke", null, ct);
    private async Task<BetaInvitationProviderResult> SendAsync(Guid productId, Guid environmentId, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path); if (body is not null) request.Content = JsonContent.Create(body); using var response = await (await ClientAsync(productId, environmentId, ct)).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) return new(null, BetaInvitationStatus.Failed, $"http_{(int)response.StatusCode}", "The external invitation provider rejected the request.");
        var dto = await response.Content.ReadFromJsonAsync<RemoteInvitation>(cancellationToken: ct) ?? throw new InvalidOperationException("The provider returned an empty response.");
        if (dto.Id == Guid.Empty) throw new InvalidOperationException("The provider returned invalid invitation metadata.");
        return new(dto.Id.ToString(), Map(dto.Status), null, null, dto.CreatedAtUtc, dto.AcceptedAtUtc, dto.RevokedAtUtc, dto.ExpiresAtUtc);
    }
    private static BetaInvitationStatus Map(string status) => status switch { "Sent" => BetaInvitationStatus.Sent, "Accepted" => BetaInvitationStatus.Accepted, "Revoked" => BetaInvitationStatus.Revoked, "Expired" => BetaInvitationStatus.Expired, "Pending" => BetaInvitationStatus.Pending, _ => BetaInvitationStatus.Failed };
    private sealed record RemoteInvitation(Guid Id, string Email, string Status, DateTime CreatedAtUtc, DateTime? InvitedAtUtc, DateTime? AcceptedAtUtc, DateTime? RevokedAtUtc, DateTime ExpiresAtUtc);
}

public sealed class BetaInvitationProviderResolver(IEnumerable<IBetaInvitationProvider> providers, IProductRepository products) : IBetaInvitationProviderResolver
{
    public async Task<IBetaInvitationProvider> ResolveAsync(Guid productId, Guid environmentId, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(productId, ct) ?? throw new InvalidOperationException("Product was not found.");
        if (environmentId == Guid.Empty) throw new InvalidOperationException("A beta provider requires an explicit environment.");
        return string.Equals(product.Slug, "cleanersflow", StringComparison.OrdinalIgnoreCase) ? providers.OfType<CleanersFlowBetaInvitationProvider>().Single() : throw new InvalidOperationException($"No beta invitation provider is registered for product '{product.Slug}'.");
    }
}
