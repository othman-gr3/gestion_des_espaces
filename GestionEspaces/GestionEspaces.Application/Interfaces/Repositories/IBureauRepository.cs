using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for offices.
/// </summary>
public interface IBureauRepository
{
    Task<Bureau?> GetByIdAsync(int idBureau, CancellationToken cancellationToken);

    Task<IReadOnlyList<Bureau>> SearchAsync(int? idBatiment, string? searchText, StatutBureau? statut, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(int? idBatiment, string? searchText, StatutBureau? statut, CancellationToken cancellationToken);

    Task<bool> ExistsByNumeroAsync(int idBatiment, string numero, int? excludingIdBureau, CancellationToken cancellationToken);

    Task AddAsync(Bureau bureau, CancellationToken cancellationToken);

    void Update(Bureau bureau);

    void Remove(Bureau bureau);

    /// <summary>
    /// Sets the expected original rowversion so EF Core can detect concurrent modifications.
    /// </summary>
    void SetOriginalVersion(Bureau bureau, byte[] version);
}