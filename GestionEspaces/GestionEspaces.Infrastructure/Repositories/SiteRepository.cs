using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

public sealed class SiteRepository : ISiteRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public SiteRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Site?> GetByIdAsync(int idSite, CancellationToken cancellationToken)
    {
        return _dbContext.Sites.SingleOrDefaultAsync(site => site.IdSite == idSite, cancellationToken);
    }

    public async Task<IReadOnlyList<Site>> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildQuery(searchText);
        return await query
            .OrderBy(site => site.Nom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? searchText, CancellationToken cancellationToken)
    {
        return BuildQuery(searchText).CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, int? excludingIdSite, CancellationToken cancellationToken)
    {
        var query = _dbContext.Sites.AsNoTracking().Where(site => site.Code == code);
        if (excludingIdSite.HasValue)
        {
            query = query.Where(site => site.IdSite != excludingIdSite.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Site site, CancellationToken cancellationToken)
    {
        return _dbContext.Sites.AddAsync(site, cancellationToken).AsTask();
    }

    public void Update(Site site)
    {
        _dbContext.Sites.Update(site);
    }

    public void Remove(Site site)
    {
        _dbContext.Sites.Remove(site);
    }

    public void SetOriginalVersion(Site site, byte[] version)
    {
        _dbContext.Entry(site).Property(s => s.Version).OriginalValue = version;
    }

    private IQueryable<Site> BuildQuery(string? searchText)
    {
        var query = _dbContext.Sites.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var filter = searchText.Trim();
            query = query.Where(site =>
                site.Nom.Contains(filter) ||
                site.Code.Contains(filter) ||
                site.Adresse.Ville.Contains(filter));
        }

        return query;
    }
}