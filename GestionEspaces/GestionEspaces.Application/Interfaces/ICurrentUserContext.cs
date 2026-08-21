namespace GestionEspaces.Application.Interfaces;

/// <summary>
/// Identifies whoever is making the current request, for audit purposes.
/// Implemented against HTTP in the Api layer so Domain/Infrastructure stay framework-free.
/// </summary>
public interface ICurrentUserContext
{
    string? Email { get; }

    string? Role { get; }
}
