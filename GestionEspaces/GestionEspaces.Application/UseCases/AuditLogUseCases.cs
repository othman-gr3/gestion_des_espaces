using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Audit;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

public sealed class AuditLogUseCases
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogUseCases(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<PagedResult<AuditLogEntryDto>>> SearchAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize is < 1 or > 100)
        {
            pageSize = 20;
        }

        var items = await _auditLogRepository.SearchAsync(pageNumber, pageSize, cancellationToken);
        var totalCount = await _auditLogRepository.CountAsync(cancellationToken);

        return Result<PagedResult<AuditLogEntryDto>>.Success(new PagedResult<AuditLogEntryDto>(items.ToArray(), pageNumber, pageSize, totalCount));
    }
}
