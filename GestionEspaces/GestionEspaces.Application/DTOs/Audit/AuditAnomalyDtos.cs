namespace GestionEspaces.Application.DTOs.Audit;

/// <summary>Severity is "info" or "warning" — the audit log only records successful business
/// actions (no failed logins), so anomalies here are unusual patterns, not confirmed incidents.</summary>
public sealed record AuditAnomalyFinding(string Severity, string Title, string Description);

public sealed record AuditAnomalyResponse(
    IReadOnlyCollection<AuditAnomalyFinding> Findings,
    bool UsedAi,
    int EntriesAnalyzed);
