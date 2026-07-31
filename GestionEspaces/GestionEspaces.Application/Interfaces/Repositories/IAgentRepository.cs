using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for agents.
/// </summary>
public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int idAgent, CancellationToken cancellationToken);

    Task<bool> ExistsByMatriculeAsync(string matricule, CancellationToken cancellationToken);

    Task AddAsync(Agent agent, CancellationToken cancellationToken);

    void Update(Agent agent);
}