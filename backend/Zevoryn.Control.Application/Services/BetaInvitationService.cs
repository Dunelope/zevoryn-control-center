namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class BetaInvitationService(IBetaCampaignRepository campaigns, IBetaInvitationRepository repository, IBetaInvitationProviderResolver providers, ISaaSEventService events) : IBetaInvitationService
{
    public async Task<IReadOnlyList<BetaInvitationDto>> GetAsync(Guid campaignId, CancellationToken cancellationToken) { await RequireCampaignAsync(campaignId, cancellationToken); return (await repository.GetAsync(campaignId, cancellationToken)).Select(Map).ToList(); }
    public async Task<BetaInvitationDto?> GetByIdAsync(Guid campaignId, Guid id, CancellationToken cancellationToken) => (await repository.GetByIdAsync(campaignId, id, cancellationToken)) is { } invitation ? Map(invitation) : null;
    public async Task<BetaInvitationDto> CreateAsync(Guid campaignId, CreateBetaInvitationRequest request, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdForUpdateAsync(campaignId, cancellationToken) ?? throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found.");
        if (!campaign.CanIssueInvitations()) throw new ConflictException("Invitations can only be created for active campaigns.");
        _ = RequireEnvironment(campaign);
        var email = BetaInvitation.NormalizeEmail(request.Email);
        var invitations = await repository.GetAsync(campaignId, cancellationToken);
        if (invitations.Any(x => x.CountsAgainstCapacity() && x.Email == email)) throw new ConflictException("An active invitation already exists for this email in the campaign.");
        if (invitations.Count(x => x.CountsAgainstCapacity()) >= campaign.MaxInvitations) throw new ConflictException("The campaign invitation capacity has been reached.");
        var invitation = BetaInvitation.Create(campaignId, email, request.ExpiresAtUtc); await repository.AddAsync(invitation, cancellationToken); await repository.SaveChangesAsync(cancellationToken);
        await PublishAsync(campaign.ProductId, "BetaInvitationCreated", invitation.Id, cancellationToken); return await ExecuteCreateAsync(campaign, invitation, "BetaInvitationSent", "BetaInvitationFailed", cancellationToken);
    }
    public async Task<BetaInvitationDto> RetryAsync(Guid campaignId, Guid invitationId, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdAsync(campaignId, cancellationToken) ?? throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found.");
        var invitation = await repository.GetByIdForUpdateAsync(campaignId, invitationId, cancellationToken) ?? throw new ResourceNotFoundException($"Invitation '{invitationId}' was not found.");
        if (invitation.Status != BetaInvitationStatus.Failed) throw new ConflictException("Only failed invitations can be retried.");
        return await ExecuteCreateAsync(campaign, invitation, "BetaInvitationRetried", "BetaInvitationFailed", cancellationToken);
    }

    private async Task<BetaInvitationDto> ExecuteCreateAsync(BetaCampaign campaign, BetaInvitation invitation, string successEvent, string failureEvent, CancellationToken ct)
    {
        var environmentId = RequireEnvironment(campaign);
        BetaInvitationProviderResult result;
        try { result = await (await providers.ResolveAsync(campaign.ProductId, environmentId, ct)).CreateInvitationAsync(new BetaInvitationProviderRequest(campaign.ProductId, environmentId, invitation.Email, invitation.Id.ToString()), ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { result = new(null, BetaInvitationStatus.Failed, "provider_unavailable", "The external invitation provider is unavailable."); }
        if (result.Status is BetaInvitationStatus.Sent or BetaInvitationStatus.Accepted)
        { invitation.MarkSent(result.ExternalReference, result.CreatedAtUtc, result.ExpiresAtUtc); if (result.Status == BetaInvitationStatus.Accepted) invitation.MarkAccepted(result.AcceptedAtUtc); await repository.SaveChangesAsync(ct); await PublishAsync(campaign.ProductId, successEvent, invitation.Id, ct); }
        else { invitation.MarkFailed(result.ErrorCode ?? "provider_failed", result.ErrorMessage ?? "The external invitation provider rejected the request."); await repository.SaveChangesAsync(ct); await PublishAsync(campaign.ProductId, failureEvent, invitation.Id, ct); }
        return Map(invitation);
    }

    public async Task RevokeAsync(Guid campaignId, Guid invitationId, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdAsync(campaignId, cancellationToken) ?? throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found.");
        var environmentId = RequireEnvironment(campaign);
        var invitation = await repository.GetByIdForUpdateAsync(campaignId, invitationId, cancellationToken) ?? throw new ResourceNotFoundException($"Invitation '{invitationId}' was not found.");
        if (!string.IsNullOrWhiteSpace(invitation.ExternalReference))
        {
            BetaInvitationProviderResult result;
            try { result = await (await providers.ResolveAsync(campaign.ProductId, environmentId, cancellationToken)).RevokeInvitationAsync(new BetaInvitationProviderRequest(campaign.ProductId, environmentId, invitation.Email, invitation.Id.ToString(), invitation.ExternalReference), cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { throw new ConflictException("The external invitation could not be revoked."); }
            if (result.Status != BetaInvitationStatus.Revoked) throw new ConflictException("The external invitation could not be revoked.");
        }
        try { invitation.Revoke(); } catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        await repository.SaveChangesAsync(cancellationToken); await PublishAsync(campaign.ProductId, "BetaInvitationRevoked", invitation.Id, cancellationToken);
    }
    public async Task<BetaInvitationDto> SyncAsync(Guid campaignId, Guid invitationId, CancellationToken ct)
    {
        var campaign = await campaigns.GetByIdAsync(campaignId, ct) ?? throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found."); var invitation = await repository.GetByIdForUpdateAsync(campaignId, invitationId, ct) ?? throw new ResourceNotFoundException($"Invitation '{invitationId}' was not found."); if (string.IsNullOrWhiteSpace(invitation.ExternalReference)) throw new ConflictException("The invitation has no external reference to synchronize.");
        BetaInvitationProviderResult result;
        try { result = await (await providers.ResolveAsync(campaign.ProductId, RequireEnvironment(campaign), ct)).GetInvitationAsync(new BetaInvitationProviderRequest(campaign.ProductId, RequireEnvironment(campaign), invitation.Email, invitation.Id.ToString(), invitation.ExternalReference), ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { throw new ConflictException("The external invitation could not be synchronized."); }
        var old = invitation.Status;
        var oldAccepted = invitation.AcceptedAtUtc;
        var oldRevoked = invitation.RevokedAtUtc;
        var oldExpires = invitation.ExpiresAtUtc;
        var oldExternal = invitation.ExternalReference;
        invitation.Synchronize(result.Status, result.AcceptedAtUtc, result.RevokedAtUtc, result.ExpiresAtUtc, result.ExternalReference, result.ErrorCode, result.ErrorMessage);
        var changed = old != invitation.Status || oldAccepted != invitation.AcceptedAtUtc || oldRevoked != invitation.RevokedAtUtc || oldExpires != invitation.ExpiresAtUtc || oldExternal != invitation.ExternalReference;
        if (changed) { await repository.SaveChangesAsync(ct); if (old != invitation.Status) await PublishAsync(campaign.ProductId, "BetaInvitationSynchronized", invitation.Id, ct); } return Map(invitation);
    }
    private async Task PublishAsync(Guid productId, string type, Guid id, CancellationToken ct) => await events.CreateAsync(new CreateSaaSEventRequest(productId, null, type, id.ToString(), System.Text.Json.JsonSerializer.Serialize(new { invitationId = id }), DateTime.UtcNow), ct);
    private static Guid RequireEnvironment(BetaCampaign campaign) => campaign.EnvironmentId ?? throw new ConflictException("This legacy campaign has no target environment; assign one before operating invitations.");
    private async Task RequireCampaignAsync(Guid campaignId, CancellationToken cancellationToken) { if (await campaigns.GetByIdAsync(campaignId, cancellationToken) is null) throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found."); }
    private static BetaInvitationDto Map(BetaInvitation i) => new(i.Id, i.BetaCampaignId, i.Email, i.Status, i.ExternalReference, i.ErrorCode, i.ErrorMessage, i.InvitedAtUtc, i.AcceptedAtUtc, i.RevokedAtUtc, i.ExpiresAtUtc, i.CreatedAtUtc, i.UpdatedAtUtc);
}

public interface IBetaInvitationService
{
    Task<IReadOnlyList<BetaInvitationDto>> GetAsync(Guid campaignId, CancellationToken cancellationToken);
    Task<BetaInvitationDto?> GetByIdAsync(Guid campaignId, Guid id, CancellationToken cancellationToken);
    Task<BetaInvitationDto> CreateAsync(Guid campaignId, CreateBetaInvitationRequest request, CancellationToken cancellationToken);
    Task<BetaInvitationDto> RetryAsync(Guid campaignId, Guid invitationId, CancellationToken cancellationToken);
    Task RevokeAsync(Guid campaignId, Guid invitationId, CancellationToken cancellationToken);
    Task<BetaInvitationDto> SyncAsync(Guid campaignId, Guid invitationId, CancellationToken cancellationToken);
}

public interface IBetaInvitationRepository
{
    Task<IReadOnlyList<BetaInvitation>> GetAsync(Guid campaignId, CancellationToken cancellationToken);
    Task<BetaInvitation?> GetByIdAsync(Guid campaignId, Guid id, CancellationToken cancellationToken);
    Task<BetaInvitation?> GetByIdForUpdateAsync(Guid campaignId, Guid id, CancellationToken cancellationToken);
    Task AddAsync(BetaInvitation invitation, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
