namespace Zevoryn.Control.Tests;

using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class BetaInvitationServiceTests
{
    [Fact]
    public async Task Retry_uses_the_invitation_snapshot_when_campaign_plan_changed()
    {
        var campaign = BetaCampaign.Create(Guid.NewGuid(), Guid.NewGuid(), "Beta", null, 1, null, null, betaPlan: BetaPlan.Growth);
        campaign.Activate();
        var invitation = BetaInvitation.Create(campaign.Id, "user@example.com", null, betaPlan: campaign.EffectiveBetaPlan);
        invitation.MarkFailed("provider_failed", "temporary failure");
        campaign.Update("Beta", null, 1, null, null, betaPlan: BetaPlan.Pro);
        var provider = new CapturingProvider();
        var service = new BetaInvitationService(new CampaignRepository(campaign), new InvitationRepository(invitation), new ProviderResolver(provider), new EventService());

        await service.RetryAsync(campaign.Id, invitation.Id, CancellationToken.None);

        Assert.Equal(BetaPlan.Growth, provider.Request!.Plan);
    }

    [Fact]
    public async Task Create_snapshots_the_campaign_plan_in_the_provider_request()
    {
        var campaign = BetaCampaign.Create(Guid.NewGuid(), Guid.NewGuid(), "Beta", null, 1, null, null, betaPlan: BetaPlan.Pro);
        campaign.Activate();
        var provider = new CapturingProvider();
        var service = new BetaInvitationService(new CampaignRepository(campaign), new InvitationRepository(), new ProviderResolver(provider), new EventService());

        var invitation = await service.CreateAsync(campaign.Id, new CreateBetaInvitationRequest("user@example.com", null), CancellationToken.None);

        Assert.Equal(BetaPlan.Pro, invitation.BetaPlan);
        Assert.Equal(BetaPlan.Pro, provider.Request!.Plan);
    }

    private sealed class CampaignRepository(BetaCampaign campaign) : IBetaCampaignRepository
    {
        public Task<IReadOnlyList<BetaCampaign>> GetAsync(Guid? _, BetaCampaignStatus? __, CancellationToken ___) => Task.FromResult<IReadOnlyList<BetaCampaign>>([campaign]);
        public Task<BetaCampaign?> GetByIdAsync(Guid id, CancellationToken _) => Task.FromResult<BetaCampaign?>(id == campaign.Id ? campaign : null);
        public Task<BetaCampaign?> GetByIdForUpdateAsync(Guid id, CancellationToken _) => Task.FromResult<BetaCampaign?>(id == campaign.Id ? campaign : null);
        public Task AddAsync(BetaCampaign _, CancellationToken __) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask;
    }

    private sealed class InvitationRepository(BetaInvitation? initial = null) : IBetaInvitationRepository
    {
        private readonly List<BetaInvitation> items = initial is null ? [] : [initial];
        public Task<IReadOnlyList<BetaInvitation>> GetAsync(Guid campaignId, CancellationToken _) => Task.FromResult<IReadOnlyList<BetaInvitation>>(items.Where(x => x.BetaCampaignId == campaignId).ToList());
        public Task<BetaInvitation?> GetByIdAsync(Guid campaignId, Guid id, CancellationToken _) => Task.FromResult(items.SingleOrDefault(x => x.BetaCampaignId == campaignId && x.Id == id));
        public Task<BetaInvitation?> GetByIdForUpdateAsync(Guid campaignId, Guid id, CancellationToken _) => Task.FromResult(items.SingleOrDefault(x => x.BetaCampaignId == campaignId && x.Id == id));
        public Task AddAsync(BetaInvitation invitation, CancellationToken _) { items.Add(invitation); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask;
    }

    private sealed class ProviderResolver(CapturingProvider provider) : IBetaInvitationProviderResolver
    {
        public Task<IBetaInvitationProvider> ResolveAsync(Guid _, Guid __, CancellationToken ___) => Task.FromResult<IBetaInvitationProvider>(provider);
    }

    private sealed class CapturingProvider : IBetaInvitationProvider
    {
        public BetaInvitationProviderRequest? Request { get; private set; }
        public Task<BetaInvitationProviderResult> CreateInvitationAsync(BetaInvitationProviderRequest request, CancellationToken _) { Request = request; return Task.FromResult(new BetaInvitationProviderResult(Guid.NewGuid().ToString(), BetaInvitationStatus.Sent, null, null)); }
        public Task<BetaInvitationProviderResult> GetInvitationAsync(BetaInvitationProviderRequest _, CancellationToken __) => Task.FromResult(new BetaInvitationProviderResult(null, BetaInvitationStatus.Sent, null, null));
        public Task<BetaInvitationProviderResult> RevokeInvitationAsync(BetaInvitationProviderRequest _, CancellationToken __) => Task.FromResult(new BetaInvitationProviderResult(null, BetaInvitationStatus.Revoked, null, null));
    }

    private sealed class EventService : ISaaSEventService
    {
        public Task<IReadOnlyList<SaaSEventDto>> GetRecentAsync(CancellationToken _) => Task.FromResult<IReadOnlyList<SaaSEventDto>>([]);
        public Task<SaaSEventDto> CreateAsync(CreateSaaSEventRequest _, CancellationToken __) => Task.FromResult<SaaSEventDto>(null!);
    }
}
