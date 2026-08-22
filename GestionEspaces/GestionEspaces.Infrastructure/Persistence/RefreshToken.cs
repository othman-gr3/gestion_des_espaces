namespace GestionEspaces.Infrastructure.Persistence;

/// <summary>
/// A persisted refresh token, keyed by the SHA-256 hash of the raw value handed to the
/// client — never the raw token itself, so a database leak doesn't leak usable tokens.
/// Lives in Infrastructure rather than Domain: authentication is a technical concern,
/// not a business entity from the cahier des charges, and users here are config-based
/// rather than a database table.
/// </summary>
public sealed class RefreshToken
{
    public int IdRefreshToken { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;

    private RefreshToken()
    {
    }

    public RefreshToken(string tokenHash, string userEmail, DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        TokenHash = tokenHash;
        UserEmail = userEmail;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Revokes this token — used both on explicit logout and on rotation at refresh time.</summary>
    public void Revoke(DateTime revokedAtUtc) => RevokedAtUtc = revokedAtUtc;
}
