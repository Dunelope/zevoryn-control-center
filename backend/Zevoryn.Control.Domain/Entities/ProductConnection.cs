namespace Zevoryn.Control.Domain.Entities;

using Zevoryn.Control.Domain.Enums;

public sealed class ProductConnection
{
    private ProductConnection() { }

    private ProductConnection(Guid id, Guid productEnvironmentId, ConnectionType connectionType, string secretReference, DateTime nowUtc)
    {
        Id = id; ProductEnvironmentId = productEnvironmentId; ConnectionType = connectionType; SecretReference = secretReference;
        IsEnabled = true; CreatedAtUtc = nowUtc; UpdatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }
    public Guid ProductEnvironmentId { get; private set; }
    public ConnectionType ConnectionType { get; private set; }
    public string SecretReference { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public DateTime? LastSuccessfulConnectionAtUtc { get; private set; }
    public DateTime? LastFailureAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public ProductEnvironment ProductEnvironment { get; private set; } = null!;

    public static ProductConnection Create(Guid productEnvironmentId, ConnectionType connectionType, string secretReference, DateTime? nowUtc = null)
    {
        if (productEnvironmentId == Guid.Empty) throw new ArgumentException("Connection must belong to an environment.", nameof(productEnvironmentId));
        ValidateSecretReference(secretReference);
        return new ProductConnection(Guid.NewGuid(), productEnvironmentId, connectionType, secretReference.Trim(), nowUtc ?? DateTime.UtcNow);
    }

    public void Update(ConnectionType connectionType, string secretReference, bool isEnabled)
    {
        ValidateSecretReference(secretReference);
        ConnectionType = connectionType; SecretReference = secretReference.Trim(); IsEnabled = isEnabled; UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordSuccess(DateTime? checkedAtUtc = null) { LastSuccessfulConnectionAtUtc = checkedAtUtc ?? DateTime.UtcNow; LastError = null; UpdatedAtUtc = DateTime.UtcNow; }
    public void RecordFailure(string error, DateTime? checkedAtUtc = null) { LastFailureAtUtc = checkedAtUtc ?? DateTime.UtcNow; LastError = error.Trim(); UpdatedAtUtc = DateTime.UtcNow; }

    private static void ValidateSecretReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Secret reference cannot be empty.", nameof(value));
        if (!System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), "^[A-Za-z0-9][A-Za-z0-9_.:-]{2,199}$"))
            throw new ArgumentException("Secret reference contains unsupported characters.", nameof(value));
    }
}
