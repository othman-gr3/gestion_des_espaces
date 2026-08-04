using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for assets.
/// </summary>
public interface IActifRepository
{
    Task<Actif?> GetByIdAsync(int idActif, CancellationToken cancellationToken);

    Task<IReadOnlyList<Actif>> SearchAsync(string? searchText, EtatActif? etat, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(string? searchText, EtatActif? etat, CancellationToken cancellationToken);

    Task<bool> ExistsByNumeroSerieAsync(string numeroSerie, int? excludingIdActif, CancellationToken cancellationToken);

    Task AddAsync(Actif actif, CancellationToken cancellationToken);

    void Update(Actif actif);

    void Remove(Actif actif);

    /// <summary>
    /// Sets the expected original rowversion so EF Core can detect concurrent modifications.
    /// </summary>
    void SetOriginalVersion(Actif actif, byte[] version);
}