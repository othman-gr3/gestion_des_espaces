using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for agent-raised requests (office change, problem reports).
/// </summary>
public interface IDemandeRepository
{
    Task<DemandeAgent?> GetByIdAsync(int idDemande, CancellationToken cancellationToken);

    Task<IReadOnlyList<DemandeAgent>> SearchAsync(StatutDemande? statut, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(StatutDemande? statut, CancellationToken cancellationToken);

    Task<IReadOnlyList<DemandeAgent>> GetByAgentIdAsync(int idAgent, CancellationToken cancellationToken);

    Task AddAsync(DemandeAgent demande, CancellationToken cancellationToken);

    void Update(DemandeAgent demande);

    /// <summary>
    /// Sets the expected original rowversion so EF Core can detect concurrent modifications.
    /// </summary>
    void SetOriginalVersion(DemandeAgent demande, byte[] version);
}
