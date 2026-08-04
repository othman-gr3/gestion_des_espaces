using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Read-only queries over affectation records (loaded via Agent aggregate for mutations).
/// </summary>
public interface IAffectationRepository
{
    /// <summary>Returns all poste affectations for an agent (active and historical).</summary>
    Task<IReadOnlyList<AffectationPoste>> GetPostesForAgentAsync(int idAgent, CancellationToken cancellationToken);

    /// <summary>Returns all actif affectations for an agent (active and historical).</summary>
    Task<IReadOnlyList<AffectationActif>> GetActifsForAgentAsync(int idAgent, CancellationToken cancellationToken);
}
