using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

public sealed class DemandeRepository : IDemandeRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public DemandeRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DemandeAgent?> GetByIdAsync(int idDemande, CancellationToken cancellationToken)
    {
        return _dbContext.Demandes
            .Include(demande => demande.Agent)
            .SingleOrDefaultAsync(demande => demande.IdDemande == idDemande, cancellationToken);
    }

    public async Task<IReadOnlyList<DemandeAgent>> SearchAsync(StatutDemande? statut, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildQuery(statut);
        return await query
            .OrderByDescending(demande => demande.DateCreation)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(StatutDemande? statut, CancellationToken cancellationToken)
    {
        return BuildQuery(statut).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DemandeAgent>> GetByAgentIdAsync(int idAgent, CancellationToken cancellationToken)
    {
        return await _dbContext.Demandes
            .Include(demande => demande.Agent)
            .Where(demande => demande.IdAgent == idAgent)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(DemandeAgent demande, CancellationToken cancellationToken)
    {
        return _dbContext.Demandes.AddAsync(demande, cancellationToken).AsTask();
    }

    public void Update(DemandeAgent demande)
    {
        _dbContext.Demandes.Update(demande);
    }

    public void SetOriginalVersion(DemandeAgent demande, byte[] version)
    {
        _dbContext.Entry(demande).Property(d => d.Version).OriginalValue = version;
    }

    private IQueryable<DemandeAgent> BuildQuery(StatutDemande? statut)
    {
        var query = _dbContext.Demandes.Include(demande => demande.Agent).AsQueryable();
        if (statut.HasValue)
        {
            query = query.Where(demande => demande.Statut == statut.Value);
        }

        return query;
    }
}
