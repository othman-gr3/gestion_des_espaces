using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Repository for reservations.
/// </summary>
public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(int idReservation, CancellationToken cancellationToken);

    Task<IReadOnlyList<Reservation>> SearchAsync(
        int? bureauId,
        int? agentId,
        DateTime? from,
        DateTime? to,
        StatutReservation? statut,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(
        int? bureauId,
        int? agentId,
        DateTime? from,
        DateTime? to,
        StatutReservation? statut,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns true if there is an active (EnAttente or Confirmee) reservation
    /// on the same bureau that overlaps with the requested time window.
    /// Pass <paramref name="excludingId"/> to ignore a specific reservation (for updates).
    /// </summary>
    Task<bool> HasOverlapAsync(
        int bureauId,
        DateTime dateDebut,
        DateTime dateFin,
        int? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);

    void Update(Reservation reservation);

    void Remove(Reservation reservation);

    void SetOriginalVersion(Reservation reservation, byte[] version);
}
