using GestionEspaces.Application.DTOs.Audit;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly GestionEspacesDbContext _dbContext;

    public AuditLogRepository(GestionEspacesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditLogEntryDto>> SearchAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        return await _dbContext.AuditLog
            .AsNoTracking()
            .OrderByDescending(entry => entry.OccurredOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new AuditLogEntryDto(
                entry.IdAuditLog,
                entry.OccurredOnUtc,
                entry.EventType,
                entry.Payload,
                entry.UtilisateurEmail,
                entry.UtilisateurRole))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.AuditLog.CountAsync(cancellationToken);
    }
}
