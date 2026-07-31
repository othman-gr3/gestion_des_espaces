using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for buildings.
/// </summary>
public interface IBatimentRepository
{
    Task<Batiment?> GetByIdAsync(int idBatiment, CancellationToken cancellationToken);

    Task<IReadOnlyList<Batiment>> SearchAsync(int? idSite, string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(int? idSite, string? searchText, CancellationToken cancellationToken);

    Task<bool> ExistsByNameForSiteAsync(int idSite, string nom, int? excludingIdBatiment, CancellationToken cancellationToken);

    Task AddAsync(Batiment batiment, CancellationToken cancellationToken);

    void Update(Batiment batiment);

    void Remove(Batiment batiment);

    /// <summary>
    /// Sets the expected original rowversion so EF Core can detect concurrent modifications.
    /// </summary>
    void SetOriginalVersion(Batiment batiment, byte[] version);
}