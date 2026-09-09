namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class BetaInvitationService(IBetaCampaignRepository campaigns, IBetaInvitationRepository repository, ISaaSEventService events) : IBetaInvitationService
{
    public async Task<IReadOnlyList<BetaInvitationDto>> GetAsync(Guid campaignId, CancellationToken cancellationToken) { await RequireCampaignAsync(campaignId, cancellationToken); return (await repository.GetAsync(campaignId, cancellationToken)).Select(Map).ToList(); }
    public async Task<BetaInvitationDto?> GetByIdAsync(Guid campaignId, Guid id, CancellationToken cancellationToken) => (await repository.GetByIdAsync(campaignId, id, cancellationToken)) is { } invitation ? Map(invitation) : null;
    public async Task<BetaInvitationDto> CreateAsync(Guid campaignId, CreateBetaInvitationRequest request, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdForUpdateAsync(campaignId, cancellationToken) ?? throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found.");
        if (!campaign.CanIssueInvitations()) throw new ConflictException("Invitations can only be created for active campaigns.");
        var email = BetaInvitation.NormalizeEmail(request.Email);
        var invitations = await repository.GetAsync(campaignId, cancellationToken);
        if (invitations.Any(x => x.CountsAgainstCapacity() && x.Email == email)) throw new ConflictException("An active invitation already exists for this email in the campaign.");
        if (invitations.Count(x => x.CountsAgainstCapacity()) >= campaign.MaxInvitations) throw new ConflictException("The campaign invitation capacity has been reached.");
        var invitation = BetaInvitation.Create(campaignId, email, request.ExpiresAtUtc); await repository.AddAsync(invitation, cancellationToken); await repository.SaveChangesAsync(cancellationToken);
        await events.CreateAsync(new CreateSaaSEventRequest(campaign.ProductId, null, "BetaInvitationCreated", invitation.Id.ToString(), "{}", DateTime.UtcNow), cancellationToken); return Map(invitation);
    }
    public async Task RevokeAsync(Guid campaignId, Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await repository.GetByIdForUpdateAsync(campaignId, invitationId, cancellationToken) ?? throw new ResourceNotFoundException($"Invitation '{invitationId}' was not found.");
        try { invitation.Revoke(); } catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        await repository.SaveChangesAsync(cancellationToken); var campaign = await campaigns.GetByIdAsync(campaignId, cancellationToken) ?? throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found.");
        await events.CreateAsync(new CreateSaaSEventRequest(campaign.ProductId, null, "BetaInvitationRevoked", invitation.Id.ToString(), "{}", DateTime.UtcNow), cancellationToken);
    }
    private async Task RequireCampaignAsync(Guid campaignId, CancellationToken cancellationToken) { if (await campaigns.GetByIdAsync(campaignId, cancellationToken) is null) throw new ResourceNotFoundException($"Campaign '{campaignId}' was not found."); }
    private static BetaInvitationDto Map(BetaInvitation i) => new(i.Id, i.BetaCampaignId, i.Email, i.Status, i.ExternalReference, i.InvitedAtUtc, i.AcceptedAtUtc, i.RevokedAtUtc, i.ExpiresAtUtc, i.CreatedAtUtc, i.UpdatedAtUtc);
}

public interface IBetaInvitationService
{
    Task<IReadOnlyList<BetaInvitationDto>> GetAsync(Guid campaignId, CancellationToken cancellationToken);
    Task<BetaInvitationDto?> GetByIdAsync(Guid campaignId, Guid id, CancellationToken cancellationToken);
    Task<BetaInvitationDto> CreateAsync(Guid campaignId, CreateBetaInvitationRequest request, CancellationToken cancellationToken);
    Task RevokeAsync(Guid campaignId, Guid invitationId, CancellationToken cancellationToken);
}

public interface IBetaInvitationRepository
{
    Task<IReadOnlyList<BetaInvitation>> GetAsync(Guid campaignId, CancellationToken cancellationToken);
    Task<BetaInvitation?> GetByIdAsync(Guid campaignId, Guid id, CancellationToken cancellationToken);
    Task<BetaInvitation?> GetByIdForUpdateAsync(Guid campaignId, Guid id, CancellationToken cancellationToken);
    Task AddAsync(BetaInvitation invitation, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
