using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for agents.
/// </summary>
public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int idAgent, CancellationToken cancellationToken);

    Task<Agent?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<Agent>> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(string? searchText, CancellationToken cancellationToken);

    Task<bool> ExistsByMatriculeAsync(string matricule, CancellationToken cancellationToken);

    Task<bool> ExistsByMatriculeAsync(string matricule, int? excludingIdAgent, CancellationToken cancellationToken);

    Task AddAsync(Agent agent, CancellationToken cancellationToken);

    void Update(Agent agent);

    void Remove(Agent agent);

    /// <summary>
    /// Sets the expected original rowversion so EF Core can detect concurrent modifications.
    /// </summary>
    void SetOriginalVersion(Agent agent, byte[] version);
}