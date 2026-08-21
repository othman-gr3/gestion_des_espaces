using GestionEspaces.Application.Interfaces;

namespace GestionEspaces.Infrastructure.Security;

/// <summary>
/// Fallback used where no HTTP-backed <see cref="ICurrentUserContext"/> is available —
/// EF Core design-time tooling and test fixtures that construct <see cref="Persistence.GestionEspacesDbContext"/>
/// directly, outside of DI.
/// </summary>
public sealed class AnonymousCurrentUserContext : ICurrentUserContext
{
    public string? Email => null;

    public string? Role => null;
}
