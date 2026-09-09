namespace Zevoryn.Control.Domain.Entities;

using Zevoryn.Control.Domain.Enums;

public sealed class BetaCampaign
{
    private BetaCampaign() { }
    private BetaCampaign(Guid id, Guid productId, Guid? environmentId, string name, string? description, int maxInvitations, DateTime? startsAtUtc, DateTime? endsAtUtc, DateTime nowUtc)
    {
        Id = id; ProductId = productId; EnvironmentId = environmentId; Name = name; Description = description; MaxInvitations = maxInvitations; StartsAtUtc = startsAtUtc; EndsAtUtc = endsAtUtc; Status = BetaCampaignStatus.Draft; CreatedAtUtc = nowUtc; UpdatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? EnvironmentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public BetaCampaignStatus Status { get; private set; }
    public int MaxInvitations { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public ICollection<BetaInvitation> Invitations { get; private set; } = new List<BetaInvitation>();

    public static BetaCampaign Create(Guid productId, Guid environmentId, string name, string? description, int maxInvitations, DateTime? startsAtUtc, DateTime? endsAtUtc, DateTime? nowUtc = null)
    {
        Validate(productId, environmentId, name, maxInvitations, startsAtUtc, endsAtUtc);
        return new BetaCampaign(Guid.NewGuid(), productId, environmentId, name.Trim(), description?.Trim(), maxInvitations, EnsureUtc(startsAtUtc), EnsureUtc(endsAtUtc), nowUtc ?? DateTime.UtcNow);
    }

    // Kept for older domain callers; application-level campaign creation always supplies an explicit target.
    public static BetaCampaign Create(Guid productId, string name, string? description, int maxInvitations, DateTime? startsAtUtc, DateTime? endsAtUtc, DateTime? nowUtc = null) => Create(productId, Guid.NewGuid(), name, description, maxInvitations, startsAtUtc, endsAtUtc, nowUtc);

    public void Update(string name, string? description, int maxInvitations, DateTime? startsAtUtc, DateTime? endsAtUtc, Guid? environmentId = null)
    {
        if (Status is BetaCampaignStatus.Closed) throw new InvalidOperationException("Closed campaigns cannot be edited.");
        if (environmentId is { } target && target != EnvironmentId && (Status == BetaCampaignStatus.Active || Invitations.Count != 0)) throw new InvalidOperationException("The target environment cannot change after activation or invitation creation.");
        if (environmentId is { } newEnvironment) { ValidateTarget(newEnvironment); EnvironmentId = newEnvironment; }
        Validate(ProductId, EnvironmentId, name, maxInvitations, startsAtUtc, endsAtUtc);
        Name = name.Trim(); Description = description?.Trim(); MaxInvitations = maxInvitations; StartsAtUtc = EnsureUtc(startsAtUtc); EndsAtUtc = EnsureUtc(endsAtUtc); UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate() { if (Status is not (BetaCampaignStatus.Draft or BetaCampaignStatus.Paused)) throw new InvalidOperationException("Only draft or paused campaigns can be activated."); Status = BetaCampaignStatus.Active; UpdatedAtUtc = DateTime.UtcNow; }
    public void Pause() { if (Status is not BetaCampaignStatus.Active) throw new InvalidOperationException("Only active campaigns can be paused."); Status = BetaCampaignStatus.Paused; UpdatedAtUtc = DateTime.UtcNow; }
    public void Close() { if (Status is not (BetaCampaignStatus.Active or BetaCampaignStatus.Paused)) throw new InvalidOperationException("Only active or paused campaigns can be closed."); Status = BetaCampaignStatus.Closed; UpdatedAtUtc = DateTime.UtcNow; }
    public bool CanIssueInvitations() => Status is BetaCampaignStatus.Active;

    private static void Validate(Guid productId, Guid? environmentId, string name, int maxInvitations, DateTime? startsAtUtc, DateTime? endsAtUtc)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Campaign must belong to a product.", nameof(productId));
        if (environmentId == Guid.Empty) throw new ArgumentException("Campaign must target an environment.", nameof(environmentId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Campaign name cannot be empty.", nameof(name));
        if (maxInvitations <= 0) throw new ArgumentOutOfRangeException(nameof(maxInvitations), "MaxInvitations must be greater than zero.");
        if (startsAtUtc is { } start && endsAtUtc is { } end && EnsureUtc(end) <= EnsureUtc(start)) throw new ArgumentException("EndsAtUtc must be after StartsAtUtc.", nameof(endsAtUtc));
    }
    private static void ValidateTarget(Guid environmentId) { if (environmentId == Guid.Empty) throw new ArgumentException("Campaign must target an environment.", nameof(environmentId)); }
    private static DateTime? EnsureUtc(DateTime? value) => value is null ? null : value.Value.Kind == DateTimeKind.Utc ? value : value.Value.ToUniversalTime();
}
