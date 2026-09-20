namespace Zevoryn.Control.Infrastructure.Integrations;

using System.Net.Http.Json;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Enums;

public sealed class CleanersFlowBetaInvitationProvider(ICleanersFlowClientResolver resolver) : IBetaInvitationProvider
{
    public Task<BetaInvitationProviderResult> CreateInvitationAsync(BetaInvitationProviderRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Plan)) throw new ArgumentException("Unsupported CleanersFlow beta plan.", nameof(request));
        return SendAsync(request.ProductId, request.EnvironmentId, HttpMethod.Post, "api/internal/control/beta-invitations", new { email = request.Email, sourceReference = request.SourceReference, plan = request.Plan.ToString() }, ct);
    }
    public Task<BetaInvitationProviderResult> GetInvitationAsync(BetaInvitationProviderRequest request, CancellationToken ct) => SendAsync(request.ProductId, request.EnvironmentId, HttpMethod.Get, $"api/internal/control/beta-invitations/{request.ExternalReference}", null, ct);
    public Task<BetaInvitationProviderResult> RevokeInvitationAsync(BetaInvitationProviderRequest request, CancellationToken ct) => SendAsync(request.ProductId, request.EnvironmentId, HttpMethod.Post, $"api/internal/control/beta-invitations/{request.ExternalReference}/revoke", null, ct);
    private async Task<BetaInvitationProviderResult> SendAsync(Guid productId, Guid environmentId, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path); if (body is not null) request.Content = JsonContent.Create(body); using var response = await (await resolver.CreateAsync(productId, environmentId, ct)).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
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
