namespace GestionEspaces.Infrastructure.Persistence;

/// <summary>
/// A login account. Lives in Infrastructure rather than Domain for the same reason as
/// <see cref="RefreshToken"/>: authentication is a technical concern, not a business
/// entity from the cahier des charges. Replaces the previous config-file-based Users
/// list in appsettings.json — a JSON file can't be safely mutated at runtime, so it
/// couldn't support self-service password changes; a database row can.
/// </summary>
public sealed class AppUser
{
    public int IdAppUser { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Image { get; private set; }

    private AppUser()
    {
    }

    public AppUser(string email, string passwordHash, string role, string name)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Name = name;
    }

    public void ChangePassword(string newPasswordHash) => PasswordHash = newPasswordHash;

    public void ChangeRole(string newRole) => Role = newRole;

    public void UpdateImage(string? image) => Image = image;
}
