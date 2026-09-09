namespace Zevoryn.Control.Tests;

using System.Net;
using System.Net.Http.Json;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Domain.Enums;

public sealed class BetaApiTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    [Fact]
    public async Task Campaign_rejects_missing_product()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/beta/campaigns", new CreateBetaCampaignRequest(Guid.NewGuid(), "Invalid Product Beta", null, 1, null, null));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Campaign_lifecycle_and_invalid_transition_are_enforced()
    {
        var product = await CreateProductAsync("beta-lifecycle");
        var environment = await CreateTargetAsync(product.Id, "beta-lifecycle");
        var response = await fixture.Client.PostAsJsonAsync("/api/beta/campaigns", new CreateBetaCampaignRequest(product.Id, "Lifecycle Beta", null, 2, null, null, environment.Id));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); var campaign = await response.Content.ReadFromJsonAsync<BetaCampaignDto>(); Assert.NotNull(campaign); Assert.Equal(BetaCampaignStatus.Draft, campaign!.Status);
        Assert.Equal(HttpStatusCode.NoContent, (await fixture.Client.PostAsync($"/api/beta/campaigns/{campaign.Id}/activate", null)).StatusCode);
        var events = await fixture.Client.GetFromJsonAsync<SaaSEventDto[]>("/api/events"); Assert.Contains(events!, x => x.Type == "BetaCampaignActivated"); Assert.DoesNotContain(events!, x => x.Type == "BetaCampaignActive");
        Assert.Equal(HttpStatusCode.NoContent, (await fixture.Client.PostAsync($"/api/beta/campaigns/{campaign.Id}/pause", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await fixture.Client.PostAsync($"/api/beta/campaigns/{campaign.Id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await fixture.Client.PostAsync($"/api/beta/campaigns/{campaign.Id}/close", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fixture.Client.PostAsync($"/api/beta/campaigns/{campaign.Id}/activate", null)).StatusCode);
    }

    [Fact]
    public async Task Invitation_requires_active_campaign_duplicate_and_capacity_rules_apply()
    {
        var product = await CreateProductAsync("beta-invitations");
        var environment = await CreateTargetAsync(product.Id, "beta-invitations");
        var campaignResponse = await fixture.Client.PostAsJsonAsync("/api/beta/campaigns", new CreateBetaCampaignRequest(product.Id, "Invitation Beta", null, 1, null, null, environment.Id));
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<BetaCampaignDto>(); Assert.NotNull(campaign);
        var beforeActive = await fixture.Client.PostAsJsonAsync($"/api/beta/campaigns/{campaign!.Id}/invitations", new CreateBetaInvitationRequest("before@example.com", null)); Assert.Equal(HttpStatusCode.Conflict, beforeActive.StatusCode);
        await fixture.Client.PostAsync($"/api/beta/campaigns/{campaign.Id}/activate", null);
        var created = await fixture.Client.PostAsJsonAsync($"/api/beta/campaigns/{campaign.Id}/invitations", new CreateBetaInvitationRequest(" User@Example.com ", null)); Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var duplicate = await fixture.Client.PostAsJsonAsync($"/api/beta/campaigns/{campaign.Id}/invitations", new CreateBetaInvitationRequest("user@example.com", null)); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var full = await fixture.Client.PostAsJsonAsync($"/api/beta/campaigns/{campaign.Id}/invitations", new CreateBetaInvitationRequest("other@example.com", null)); Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        var invitation = await created.Content.ReadFromJsonAsync<BetaInvitationDto>(); Assert.NotNull(invitation);
        Assert.Equal(HttpStatusCode.NoContent, (await fixture.Client.PostAsync($"/api/beta/campaigns/{campaign.Id}/invitations/{invitation!.Id}/revoke", null)).StatusCode);
        var available = await fixture.Client.PostAsJsonAsync($"/api/beta/campaigns/{campaign.Id}/invitations", new CreateBetaInvitationRequest("other@example.com", null)); Assert.Equal(HttpStatusCode.OK, available.StatusCode);
        var events = await fixture.Client.GetFromJsonAsync<SaaSEventDto[]>("/api/events"); Assert.Contains(events!, x => x.Type == "BetaInvitationCreated"); Assert.Contains(events!, x => x.Type == "BetaInvitationRevoked");
    }

    [Fact]
    public async Task Campaign_filters_by_product_and_status()
    {
        var product = await CreateProductAsync("beta-filter"); var environment = await CreateTargetAsync(product.Id, "beta-filter"); await fixture.Client.PostAsJsonAsync("/api/beta/campaigns", new CreateBetaCampaignRequest(product.Id, "Filter Beta", null, 1, null, null, environment.Id));
        var filtered = await fixture.Client.GetFromJsonAsync<BetaCampaignDto[]>($"/api/beta/campaigns?productId={product.Id}&status=Draft"); Assert.NotNull(filtered); Assert.Contains(filtered!, x => x.Name == "Filter Beta");
    }

    [Fact]
    public async Task Campaign_rejects_environment_from_another_product()
    {
        var product = await CreateProductAsync("beta-target"); var other = await CreateProductAsync("beta-other"); var environment = await CreateTargetAsync(other.Id, "beta-other");
        var response = await fixture.Client.PostAsJsonAsync("/api/beta/campaigns", new CreateBetaCampaignRequest(product.Id, "Wrong Target", null, 1, null, null, environment.Id));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Campaign_rejects_missing_environment()
    {
        var product = await CreateProductAsync("beta-no-target");
        var response = await fixture.Client.PostAsJsonAsync("/api/beta/campaigns", new CreateBetaCampaignRequest(product.Id, "No Target", null, 1, null, null));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<ProductDto> CreateProductAsync(string prefix) { var response = await fixture.Client.PostAsJsonAsync("/api/products", new CreateProductRequest($"{prefix} product", $"{prefix}-{Guid.NewGuid():N}", null)); response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<ProductDto>())!; }
    private async Task<ProductEnvironmentDto> CreateTargetAsync(Guid productId, string prefix)
    {
        var environmentResponse = await fixture.Client.PostAsJsonAsync($"/api/products/{productId}/environments", new CreateProductEnvironmentRequest($"{prefix} environment", EnvironmentType.Development, "http://localhost:5080"));
        environmentResponse.EnsureSuccessStatusCode(); var environment = (await environmentResponse.Content.ReadFromJsonAsync<ProductEnvironmentDto>())!;
        var connectionResponse = await fixture.Client.PostAsJsonAsync($"/api/products/{productId}/environments/{environment.Id}/connections", new CreateProductConnectionRequest(ConnectionType.InternalApi, $"{prefix}-control-api"));
        connectionResponse.EnsureSuccessStatusCode(); return environment;
    }
}
