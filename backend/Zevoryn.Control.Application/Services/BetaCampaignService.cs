namespace Zevoryn.Control.Application.Services;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Exceptions;
using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class BetaCampaignService(IProductRepository products, IBetaCampaignRepository repository, ISaaSEventService events) : IBetaCampaignService
{
    public async Task<IReadOnlyList<BetaCampaignDto>> GetAsync(Guid? productId, BetaCampaignStatus? status, CancellationToken cancellationToken) => (await repository.GetAsync(productId, status, cancellationToken)).Select(Map).ToList();
    public async Task<BetaCampaignDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => (await repository.GetByIdAsync(id, cancellationToken)) is { } campaign ? Map(campaign) : null;
    public async Task<BetaCampaignDto> CreateAsync(CreateBetaCampaignRequest request, CancellationToken cancellationToken)
    {
        if (await products.GetByIdAsync(request.ProductId, cancellationToken) is null) throw new ResourceNotFoundException($"Product '{request.ProductId}' was not found.");
        var campaign = BetaCampaign.Create(request.ProductId, request.Name, request.Description, request.MaxInvitations, request.StartsAtUtc, request.EndsAtUtc);
        await repository.AddAsync(campaign, cancellationToken); await repository.SaveChangesAsync(cancellationToken);
        await PublishAsync(campaign, "BetaCampaignCreated", cancellationToken); return Map(campaign);
    }
    public async Task<BetaCampaignDto> UpdateAsync(Guid id, UpdateBetaCampaignRequest request, CancellationToken cancellationToken)
    {
        var campaign = await repository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new ResourceNotFoundException($"Campaign '{id}' was not found.");
        try { campaign.Update(request.Name, request.Description, request.MaxInvitations, request.StartsAtUtc, request.EndsAtUtc); } catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        await repository.SaveChangesAsync(cancellationToken); return Map(campaign);
    }
    public Task ActivateAsync(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, BetaCampaignStatus.Active, cancellationToken);
    public Task PauseAsync(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, BetaCampaignStatus.Paused, cancellationToken);
    public Task CloseAsync(Guid id, CancellationToken cancellationToken) => TransitionAsync(id, BetaCampaignStatus.Closed, cancellationToken);

    private async Task TransitionAsync(Guid id, BetaCampaignStatus target, CancellationToken cancellationToken)
    {
        var campaign = await repository.GetByIdForUpdateAsync(id, cancellationToken) ?? throw new ResourceNotFoundException($"Campaign '{id}' was not found.");
        try { if (target == BetaCampaignStatus.Active) campaign.Activate(); else if (target == BetaCampaignStatus.Paused) campaign.Pause(); else campaign.Close(); } catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        await repository.SaveChangesAsync(cancellationToken); await PublishAsync(campaign, target == BetaCampaignStatus.Active ? "BetaCampaignActivated" : $"BetaCampaign{target}", cancellationToken);
    }
    private async Task PublishAsync(BetaCampaign campaign, string type, CancellationToken cancellationToken) => await events.CreateAsync(new CreateSaaSEventRequest(campaign.ProductId, null, type, campaign.Id.ToString(), "{}", DateTime.UtcNow), cancellationToken);
    private static BetaCampaignDto Map(BetaCampaign c) { var active = c.Invitations.Where(i => i.CountsAgainstCapacity()).ToList(); return new(c.Id, c.ProductId, c.Name, c.Description, c.Status, c.MaxInvitations, c.Invitations.Count, c.Invitations.Count(i => i.Status == BetaInvitationStatus.Pending), c.Invitations.Count(i => i.Status == BetaInvitationStatus.Sent), c.Invitations.Count(i => i.Status == BetaInvitationStatus.Accepted), c.Invitations.Count(i => i.Status == BetaInvitationStatus.Revoked), c.Invitations.Count(i => i.Status == BetaInvitationStatus.Failed), Math.Max(0, c.MaxInvitations - active.Count), c.StartsAtUtc, c.EndsAtUtc, c.CreatedAtUtc, c.UpdatedAtUtc); }
}

public interface IBetaCampaignService
{
    Task<IReadOnlyList<BetaCampaignDto>> GetAsync(Guid? productId, BetaCampaignStatus? status, CancellationToken cancellationToken);
    Task<BetaCampaignDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<BetaCampaignDto> CreateAsync(CreateBetaCampaignRequest request, CancellationToken cancellationToken);
    Task<BetaCampaignDto> UpdateAsync(Guid id, UpdateBetaCampaignRequest request, CancellationToken cancellationToken);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken);
    Task PauseAsync(Guid id, CancellationToken cancellationToken);
    Task CloseAsync(Guid id, CancellationToken cancellationToken);
}

public interface IBetaCampaignRepository
{
    Task<IReadOnlyList<BetaCampaign>> GetAsync(Guid? productId, BetaCampaignStatus? status, CancellationToken cancellationToken);
    Task<BetaCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<BetaCampaign?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(BetaCampaign campaign, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
