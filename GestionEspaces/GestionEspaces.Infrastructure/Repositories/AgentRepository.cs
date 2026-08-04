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

    public Task<Agent?> GetByIdAsync(int idAgent, CancellationToken cancellationToken)
    {
        return _dbContext.Agents
            .Include(agent => agent.AffectationsPoste)
            .Include(agent => agent.AffectationsActif)
            .SingleOrDefaultAsync(agent => agent.IdAgent == idAgent, cancellationToken);
    }

    public async Task<IReadOnlyList<Agent>> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildQuery(searchText);
        return await query
            .OrderBy(agent => agent.Nom)
            .ThenBy(agent => agent.Prenom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchText, CancellationToken cancellationToken)
    {
        return BuildQuery(searchText).CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByMatriculeAsync(string matricule, CancellationToken cancellationToken)
    {
        return _dbContext.Agents.AsNoTracking()
            .AnyAsync(agent => agent.Matricule == matricule, cancellationToken);
    }

    public Task<bool> ExistsByMatriculeAsync(string matricule, int? excludingIdAgent, CancellationToken cancellationToken)
    {
        var query = _dbContext.Agents.AsNoTracking().Where(agent => agent.Matricule == matricule);
        if (excludingIdAgent.HasValue)
        {
            query = query.Where(agent => agent.IdAgent != excludingIdAgent.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Agent agent, CancellationToken cancellationToken)
    {
        return _dbContext.Agents.AddAsync(agent, cancellationToken).AsTask();
    }

    public void Update(Agent agent)
    {
        _dbContext.Agents.Update(agent);
    }

    public void Remove(Agent agent)
    {
        _dbContext.Agents.Remove(agent);
    }

    public void SetOriginalVersion(Agent agent, byte[] version)
    {
        _dbContext.Entry(agent).Property(a => a.Version).OriginalValue = version;
    }

    private IQueryable<Agent> BuildQuery(string? searchText)
    {
        var query = _dbContext.Agents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var filter = searchText.Trim();
            query = query.Where(agent =>
                agent.Nom.Contains(filter) ||
                agent.Prenom.Contains(filter) ||
                agent.Matricule.Contains(filter) ||
                (agent.Departement != null && agent.Departement.Contains(filter)));
        }

        return query;
    }
}