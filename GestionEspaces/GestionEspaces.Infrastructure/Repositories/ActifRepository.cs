using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

/// <summary>
/// EF Core actif repository.
/// </summary>
public sealed class ActifRepository : IActifRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public ActifRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Actif?> GetByIdAsync(int idActif, CancellationToken cancellationToken)
    {
        return _dbContext.Actifs
            .Include(actif => actif.Affectations)
            .SingleOrDefaultAsync(actif => actif.IdActif == idActif, cancellationToken);
    }

    public async Task<IReadOnlyList<Actif>> SearchAsync(string? searchText, EtatActif? etat, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildQuery(searchText, etat);
        return await query
            .OrderBy(actif => actif.Nom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchText, EtatActif? etat, CancellationToken cancellationToken)
    {
        return BuildQuery(searchText, etat).CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByNumeroSerieAsync(string numeroSerie, int? excludingIdActif, CancellationToken cancellationToken)
    {
        var query = _dbContext.Actifs.AsNoTracking().Where(actif => actif.NumeroSerie == numeroSerie);
        if (excludingIdActif.HasValue)
        {
            query = query.Where(actif => actif.IdActif != excludingIdActif.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Actif actif, CancellationToken cancellationToken)
    {
        return _dbContext.Actifs.AddAsync(actif, cancellationToken).AsTask();
    }

    public void Update(Actif actif)
    {
        _dbContext.Actifs.Update(actif);
    }

    public void Remove(Actif actif)
    {
        _dbContext.Actifs.Remove(actif);
    }

    public void SetOriginalVersion(Actif actif, byte[] version)
    {
        _dbContext.Entry(actif).Property(a => a.Version).OriginalValue = version;
    }

    private IQueryable<Actif> BuildQuery(string? searchText, EtatActif? etat)
    {
        var query = _dbContext.Actifs.AsQueryable();
        if (etat.HasValue)
        {
            query = query.Where(actif => actif.Etat == etat.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var filter = searchText.Trim();
            query = query.Where(actif =>
                actif.Nom.Contains(filter)
                || (actif.Type != null && actif.Type.Contains(filter))
                || (actif.NumeroSerie != null && actif.NumeroSerie.Contains(filter)));
        }

        return query;
    }
}