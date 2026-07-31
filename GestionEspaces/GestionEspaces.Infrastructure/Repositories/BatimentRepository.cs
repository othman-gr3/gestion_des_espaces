using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

public sealed class BatimentRepository : IBatimentRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public BatimentRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Batiment?> GetByIdAsync(int idBatiment, CancellationToken cancellationToken)
    {
        return _dbContext.Batiments.SingleOrDefaultAsync(batiment => batiment.IdBatiment == idBatiment, cancellationToken);
    }

    public async Task<IReadOnlyList<Batiment>> SearchAsync(int? idSite, string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildQuery(idSite, searchText);
        return await query
            .OrderBy(batiment => batiment.Nom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(int? idSite, string? searchText, CancellationToken cancellationToken)
    {
        return BuildQuery(idSite, searchText).CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameForSiteAsync(int idSite, string nom, int? excludingIdBatiment, CancellationToken cancellationToken)
    {
        var query = _dbContext.Batiments.AsNoTracking()
            .Where(batiment => batiment.IdSite == idSite && batiment.Nom == nom);

        if (excludingIdBatiment.HasValue)
        {
            query = query.Where(batiment => batiment.IdBatiment != excludingIdBatiment.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Batiment batiment, CancellationToken cancellationToken)
    {
        return _dbContext.Batiments.AddAsync(batiment, cancellationToken).AsTask();
    }

    public void Update(Batiment batiment)
    {
        _dbContext.Batiments.Update(batiment);
    }

    public void Remove(Batiment batiment)
    {
        _dbContext.Batiments.Remove(batiment);
    }

    public void SetOriginalVersion(Batiment batiment, byte[] version)
    {
        _dbContext.Entry(batiment).Property(b => b.Version).OriginalValue = version;
    }

    private IQueryable<Batiment> BuildQuery(int? idSite, string? searchText)
    {
        var query = _dbContext.Batiments.AsQueryable();
        if (idSite.HasValue)
        {
            query = query.Where(batiment => batiment.IdSite == idSite.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var filter = searchText.Trim();
            query = query.Where(batiment => batiment.Nom.Contains(filter));
        }

        return query;
    }
}