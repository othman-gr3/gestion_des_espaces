using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

/// <summary>
/// EF Core agent repository.
/// </summary>
public sealed class AgentRepository : IAgentRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public AgentRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByMatriculeAsync(string matricule, CancellationToken cancellationToken)
    {
        return _dbContext.Agents.AsNoTracking().AnyAsync(agent => agent.Matricule == matricule, cancellationToken);
    }

    public Task<Agent?> GetByIdAsync(int idAgent, CancellationToken cancellationToken)
    {
        return _dbContext.Agents
            .Include(agent => agent.AffectationsPoste)
            .Include(agent => agent.AffectationsActif)
            .SingleOrDefaultAsync(agent => agent.IdAgent == idAgent, cancellationToken);
    }

    public Task AddAsync(Agent agent, CancellationToken cancellationToken)
    {
        return _dbContext.Agents.AddAsync(agent, cancellationToken).AsTask();
    }

    public void Update(Agent agent)
    {
        _dbContext.Agents.Update(agent);
    }
}