namespace Zevoryn.Control.Domain.Entities;

using System.Net.Mail;
using Zevoryn.Control.Domain.Enums;

public sealed class BetaInvitation
{
    private BetaInvitation() { }
    private BetaInvitation(Guid id, Guid campaignId, string email, DateTime? expiresAtUtc, DateTime nowUtc)
    {
        Id = id; BetaCampaignId = campaignId; Email = email; Status = BetaInvitationStatus.Pending; ExpiresAtUtc = expiresAtUtc; InvitedAtUtc = nowUtc; CreatedAtUtc = nowUtc; UpdatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid BetaCampaignId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public BetaInvitationStatus Status { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? InvitedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public BetaCampaign BetaCampaign { get; private set; } = null!;

    public static BetaInvitation Create(Guid campaignId, string email, DateTime? expiresAtUtc, DateTime? nowUtc = null)
    {
        if (campaignId == Guid.Empty) throw new ArgumentException("Invitation must belong to a campaign.", nameof(campaignId));
        return new BetaInvitation(Guid.NewGuid(), campaignId, NormalizeEmail(email), EnsureUtc(expiresAtUtc), nowUtc ?? DateTime.UtcNow);
    }

    public void SetExternalReference(string? externalReference) { ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim(); UpdatedAtUtc = DateTime.UtcNow; }
    public void MarkSent(string? externalReference = null, DateTime? invitedAtUtc = null, DateTime? expiresAtUtc = null) { EnsureMutable(); Status = BetaInvitationStatus.Sent; ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? ExternalReference : externalReference.Trim(); InvitedAtUtc = EnsureUtc(invitedAtUtc) ?? DateTime.UtcNow; ExpiresAtUtc = EnsureUtc(expiresAtUtc) ?? ExpiresAtUtc; ErrorCode = null; ErrorMessage = null; UpdatedAtUtc = DateTime.UtcNow; }
    public void MarkFailed(string errorCode, string errorMessage) { if (string.IsNullOrWhiteSpace(errorCode)) throw new ArgumentException("Error code cannot be empty.", nameof(errorCode)); Status = BetaInvitationStatus.Failed; ErrorCode = errorCode.Trim()[..Math.Min(80, errorCode.Trim().Length)]; ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "The external invitation provider failed." : errorMessage.Trim()[..Math.Min(500, errorMessage.Trim().Length)]; UpdatedAtUtc = DateTime.UtcNow; }
    public void MarkExpired(DateTime? expiredAtUtc = null) { if (Status == BetaInvitationStatus.Revoked) throw new InvalidOperationException("A revoked invitation cannot expire."); Status = BetaInvitationStatus.Expired; ExpiresAtUtc = EnsureUtc(expiredAtUtc) ?? ExpiresAtUtc; UpdatedAtUtc = DateTime.UtcNow; }
    public void MarkAccepted(DateTime? acceptedAtUtc = null) { EnsureMutable(); Status = BetaInvitationStatus.Accepted; AcceptedAtUtc = EnsureUtc(acceptedAtUtc) ?? DateTime.UtcNow; UpdatedAtUtc = DateTime.UtcNow; }
    public void Revoke() { if (Status is BetaInvitationStatus.Revoked or BetaInvitationStatus.Expired) throw new InvalidOperationException("This invitation is already inactive."); Status = BetaInvitationStatus.Revoked; RevokedAtUtc = DateTime.UtcNow; UpdatedAtUtc = DateTime.UtcNow; }
    public void Synchronize(BetaInvitationStatus status, DateTime? acceptedAtUtc = null, DateTime? revokedAtUtc = null, DateTime? expiresAtUtc = null, string? externalReference = null, string? errorCode = null, string? errorMessage = null)
    {
        Status = status;
        if (!string.IsNullOrWhiteSpace(externalReference)) ExternalReference = externalReference.Trim();
        AcceptedAtUtc = EnsureUtc(acceptedAtUtc) ?? AcceptedAtUtc;
        RevokedAtUtc = EnsureUtc(revokedAtUtc) ?? RevokedAtUtc;
        ExpiresAtUtc = EnsureUtc(expiresAtUtc) ?? ExpiresAtUtc;
        if (status == BetaInvitationStatus.Failed) { ErrorCode = errorCode; ErrorMessage = errorMessage; }
        else { ErrorCode = null; ErrorMessage = null; }
        UpdatedAtUtc = DateTime.UtcNow;
    }
    public bool CountsAgainstCapacity() => Status is not (BetaInvitationStatus.Revoked or BetaInvitationStatus.Expired);

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be empty.", nameof(email));
        var normalized = email.Trim().ToLowerInvariant();
        try { _ = new MailAddress(normalized); } catch (FormatException) { throw new ArgumentException("Email must be valid.", nameof(email)); }
        return normalized;
    }
    private void EnsureMutable() { if (Status is BetaInvitationStatus.Revoked or BetaInvitationStatus.Expired) throw new InvalidOperationException("This invitation is no longer mutable."); }
    private static DateTime? EnsureUtc(DateTime? value) => value is null ? null : value.Value.Kind == DateTimeKind.Utc ? value : value.Value.ToUniversalTime();
}
