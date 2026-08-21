using GestionEspaces.Application.DTOs.Audit;

namespace GestionEspaces.Application.Interfaces.Repositories;

/// <summary>
/// Read-only access to the audit trail. Returns the DTO directly rather than a Domain
/// entity — the audit log is a cross-cutting infrastructure concern, not a business
/// concept from the cahier des charges, so it has no Domain-layer representation to map.
/// </summary>
public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLogEntryDto>> SearchAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);
}
