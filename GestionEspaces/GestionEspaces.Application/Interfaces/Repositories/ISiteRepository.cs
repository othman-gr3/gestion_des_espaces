using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for sites.
/// </summary>
public interface ISiteRepository
{
    Task<Site?> GetByIdAsync(int idSite, CancellationToken cancellationToken);

    Task<IReadOnlyList<Site>> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(string? searchText, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, int? excludingIdSite, CancellationToken cancellationToken);

    Task AddAsync(Site site, CancellationToken cancellationToken);

    void Update(Site site);

    void Remove(Site site);

    /// <summary>
    /// Sets the expected original rowversion so EF Core can detect concurrent modifications.
    /// </summary>
    void SetOriginalVersion(Site site, byte[] version);
}