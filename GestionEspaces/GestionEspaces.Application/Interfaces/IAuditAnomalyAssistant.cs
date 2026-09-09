using GestionEspaces.Application.DTOs.Audit;

namespace GestionEspaces.Application.Interfaces;

/// <summary>
/// Reviews a batch of recent audit log entries for unusual patterns (activity bursts,
/// off-hours actions, etc.) via an LLM. Implemented against OpenRouter in Infrastructure.
/// Returns null on any failure so the caller can fall back to a deterministic, rule-based
/// heuristic instead.
/// </summary>
public interface IAuditAnomalyAssistant
{
    Task<IReadOnlyList<AuditAnomalyFinding>?> AnalyzeAsync(IReadOnlyList<AuditLogEntryDto> entries, CancellationToken cancellationToken);
}
