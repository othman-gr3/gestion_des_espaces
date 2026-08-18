using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

/// <summary>
/// EF Core bureau repository.
/// </summary>
public sealed class BureauRepository : IBureauRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public BureauRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Bureau?> GetByIdAsync(int idBureau, CancellationToken cancellationToken)
    {
        return _dbContext.Bureaux
            .Include(bureau => bureau.Affectations)
            .SingleOrDefaultAsync(bureau => bureau.IdBureau == idBureau, cancellationToken);
    }

    public async Task<IReadOnlyList<Bureau>> SearchAsync(int? idBatiment, string? searchText, StatutBureau? statut, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildQuery(idBatiment, searchText, statut);
        return await query
            .OrderBy(bureau => bureau.Numero)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(int? idBatiment, string? searchText, StatutBureau? statut, CancellationToken cancellationToken)
    {
        return BuildQuery(idBatiment, searchText, statut).CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByNumeroAsync(int idBatiment, string numero, int? excludingIdBureau, CancellationToken cancellationToken)
    {
        var query = _dbContext.Bureaux.AsNoTracking()
            .Where(bureau => bureau.IdBatiment == idBatiment && bureau.Numero == numero);

        if (excludingIdBureau.HasValue)
        {
            query = query.Where(bureau => bureau.IdBureau != excludingIdBureau.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Bureau bureau, CancellationToken cancellationToken)
    {
        return _dbContext.Bureaux.AddAsync(bureau, cancellationToken).AsTask();
    }

    public void Update(Bureau bureau)
    {
        _dbContext.Bureaux.Update(bureau);
    }

    public void Remove(Bureau bureau)
    {
        _dbContext.Bureaux.Remove(bureau);
    }

    public void SetOriginalVersion(Bureau bureau, byte[] version)
    {
        _dbContext.Entry(bureau).Property(b => b.Version).OriginalValue = version;
    }

    private IQueryable<Bureau> BuildQuery(int? idBatiment, string? searchText, StatutBureau? statut)
    {
        var query = _dbContext.Bureaux.AsQueryable();
        if (idBatiment.HasValue)
        {
            query = query.Where(bureau => bureau.IdBatiment == idBatiment.Value);
        }

        if (statut.HasValue)
        {
            query = query.Where(bureau => bureau.Statut == statut.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var filter = searchText.Trim();
            query = query.Where(bureau => bureau.Numero.Contains(filter));
        }

        return query;
    }
}