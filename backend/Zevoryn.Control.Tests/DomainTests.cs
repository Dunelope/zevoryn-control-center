namespace Zevoryn.Control.Tests;

using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;

public sealed class DomainTests
{
    [Fact] public void Product_creation_normalizes_slug() { var p = Product.Create("CleanersFlow", " CleanersFlow "); Assert.Equal("cleanersflow", p.Slug); }
    [Fact] public void Product_rejects_empty_name() => Assert.Throws<ArgumentException>(() => Product.Create("", "product"));
    [Fact] public void Product_rejects_invalid_slug() => Assert.Throws<ArgumentException>(() => Product.Create("Product", "not valid"));
    [Fact] public void Environment_accepts_absolute_url() { var e = ProductEnvironment.Create(Guid.NewGuid(), "Production", EnvironmentType.Production, "https://example.com"); Assert.Equal("https://example.com/", e.BaseUrl); }
    [Fact] public void Environment_rejects_invalid_url() => Assert.Throws<ArgumentException>(() => ProductEnvironment.Create(Guid.NewGuid(), "Production", EnvironmentType.Production, "localhost"));
    [Fact] public void Event_accepts_valid_payload() { var e = SaaSEvent.Create(Guid.NewGuid(), null, "CustomerCreated", "c-1", "{}", DateTime.UtcNow); Assert.Equal("CustomerCreated", e.Type); }
    [Fact] public void Event_rejects_empty_type() => Assert.Throws<ArgumentException>(() => SaaSEvent.Create(Guid.NewGuid(), null, "", null, "{}", DateTime.UtcNow));
    [Fact] public void Beta_campaign_allows_only_valid_transitions() { var campaign = BetaCampaign.Create(Guid.NewGuid(), "Private Beta", null, 2, null, null); campaign.Activate(); campaign.Pause(); campaign.Activate(); campaign.Close(); Assert.Equal(BetaCampaignStatus.Closed, campaign.Status); Assert.Throws<InvalidOperationException>(campaign.Activate); }
    [Fact] public void Beta_campaign_rejects_invalid_capacity_and_dates() { Assert.Throws<ArgumentOutOfRangeException>(() => BetaCampaign.Create(Guid.NewGuid(), "Beta", null, 0, null, null)); var instant = DateTime.UtcNow; Assert.Throws<ArgumentException>(() => BetaCampaign.Create(Guid.NewGuid(), "Beta", null, 1, instant, instant)); }
    [Theory]
    [InlineData(BetaPlan.Starter)]
    [InlineData(BetaPlan.Growth)]
    [InlineData(BetaPlan.Pro)]
    public void Beta_campaign_supports_the_explicit_founding_plans(BetaPlan plan) { var campaign = BetaCampaign.Create(Guid.NewGuid(), Guid.NewGuid(), "Beta", null, 1, null, null, betaPlan: plan); Assert.Equal(plan, campaign.BetaPlan); Assert.Equal(plan, campaign.EffectiveBetaPlan); }
    [Fact] public void Beta_campaign_omitted_plan_defaults_to_starter() { var campaign = BetaCampaign.Create(Guid.NewGuid(), Guid.NewGuid(), "Beta", null, 1, null, null); Assert.Equal(BetaPlan.Starter, campaign.EffectiveBetaPlan); }
    [Fact] public void Beta_invitation_snapshots_the_campaign_plan_and_does_not_follow_campaign_changes() { var campaign = BetaCampaign.Create(Guid.NewGuid(), Guid.NewGuid(), "Beta", null, 2, null, null, betaPlan: BetaPlan.Growth); var invitation = BetaInvitation.Create(campaign.Id, "user@example.com", null, betaPlan: campaign.EffectiveBetaPlan); campaign.Invitations.Add(invitation); campaign.Update("Beta", null, 2, null, null, betaPlan: BetaPlan.Pro); Assert.Equal(BetaPlan.Growth, invitation.EffectiveBetaPlan); Assert.Equal(BetaPlan.Pro, campaign.EffectiveBetaPlan); }
    [Fact] public void Legacy_null_plan_falls_back_to_starter() { var campaign = BetaCampaign.Create(Guid.NewGuid(), Guid.NewGuid(), "Beta", null, 1, null, null); var invitation = BetaInvitation.Create(campaign.Id, "user@example.com", null); typeof(BetaCampaign).GetProperty(nameof(BetaCampaign.BetaPlan))!.SetValue(campaign, null); typeof(BetaInvitation).GetProperty(nameof(BetaInvitation.BetaPlan))!.SetValue(invitation, null); Assert.Equal(BetaPlan.Starter, campaign.EffectiveBetaPlan); Assert.Equal(BetaPlan.Starter, invitation.EffectiveBetaPlan); }
    [Fact] public void Beta_invitation_normalizes_email_and_never_has_token_field() { var invitation = BetaInvitation.Create(Guid.NewGuid(), "  USER@Example.COM ", null); Assert.Equal("user@example.com", invitation.Email); invitation.SetExternalReference("external-123"); Assert.Equal("external-123", invitation.ExternalReference); Assert.DoesNotContain("token", string.Join(',', typeof(BetaInvitation).GetProperties().Select(x => x.Name)), StringComparison.OrdinalIgnoreCase); }
}
