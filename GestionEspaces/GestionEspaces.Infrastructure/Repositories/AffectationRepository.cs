using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

/// <summary>
/// EF Core affectation repository for query-only access.
/// Mutations go through the Agent aggregate via AgentRepository.
/// </summary>
public sealed class AffectationRepository : IAffectationRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public AffectationRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AffectationPoste>> GetPostesForAgentAsync(int idAgent, CancellationToken cancellationToken)
    {
        return await _dbContext.AffectationsPoste
            .Include(a => a.Bureau)
            .Where(a => a.IdAgent == idAgent)
            .OrderByDescending(a => a.DateAffectation)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AffectationActif>> GetActifsForAgentAsync(int idAgent, CancellationToken cancellationToken)
    {
        return await _dbContext.AffectationsActif
            .Include(a => a.Actif)
            .Where(a => a.IdAgent == idAgent)
            .OrderByDescending(a => a.DateAffectation)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
