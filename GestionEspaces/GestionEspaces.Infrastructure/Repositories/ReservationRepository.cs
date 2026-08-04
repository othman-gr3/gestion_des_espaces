using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Domain.Entities;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

/// <summary>
/// EF Core reservation repository.
/// </summary>
public sealed class ReservationRepository : IReservationRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public ReservationRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Reservation?> GetByIdAsync(int idReservation, CancellationToken cancellationToken)
    {
        return _dbContext.Reservations
            .SingleOrDefaultAsync(r => r.IdReservation == idReservation, cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> SearchAsync(
        int? bureauId, int? agentId, DateTime? from, DateTime? to, StatutReservation? statut,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        return await BuildQuery(bureauId, agentId, from, to, statut)
            .OrderBy(r => r.DateDebut)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        int? bureauId, int? agentId, DateTime? from, DateTime? to, StatutReservation? statut,
        CancellationToken cancellationToken)
    {
        return BuildQuery(bureauId, agentId, from, to, statut).CountAsync(cancellationToken);
    }

    public Task<bool> HasOverlapAsync(
        int bureauId, DateTime dateDebut, DateTime dateFin, int? excludingId,
        CancellationToken cancellationToken)
    {
        var activeStatuts = new[] { StatutReservation.EnAttente, StatutReservation.Confirmee };

        var query = _dbContext.Reservations
            .Where(r => r.IdBureau == bureauId)
            .Where(r => activeStatuts.Contains(r.Statut))
            .Where(r => r.DateDebut < dateFin && r.DateFin > dateDebut);   // overlap condition

        if (excludingId.HasValue)
        {
            query = query.Where(r => r.IdReservation != excludingId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        return _dbContext.Reservations.AddAsync(reservation, cancellationToken).AsTask();
    }

    public void Update(Reservation reservation)
    {
        _dbContext.Reservations.Update(reservation);
    }

    public void Remove(Reservation reservation)
    {
        _dbContext.Reservations.Remove(reservation);
    }

    public void SetOriginalVersion(Reservation reservation, byte[] version)
    {
        _dbContext.Entry(reservation).Property(r => r.Version).OriginalValue = version;
    }

    private IQueryable<Reservation> BuildQuery(
        int? bureauId, int? agentId, DateTime? from, DateTime? to, StatutReservation? statut)
    {
        var query = _dbContext.Reservations.AsQueryable();

        if (bureauId.HasValue) query = query.Where(r => r.IdBureau == bureauId.Value);
        if (agentId.HasValue) query = query.Where(r => r.IdAgent == agentId.Value);
        if (from.HasValue) query = query.Where(r => r.DateFin >= from.Value);
        if (to.HasValue) query = query.Where(r => r.DateDebut <= to.Value);
        if (statut.HasValue) query = query.Where(r => r.Statut == statut.Value);

        return query;
    }
}
