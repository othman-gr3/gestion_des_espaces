using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.Audit;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;

namespace GestionEspaces.Application.UseCases;

public sealed class AuditLogUseCases
{
    private const int AnomalyWindowSize = 300;

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditAnomalyAssistant _anomalyAssistant;

    public AuditLogUseCases(IAuditLogRepository auditLogRepository, IAuditAnomalyAssistant anomalyAssistant)
    {
        _auditLogRepository = auditLogRepository;
        _anomalyAssistant = anomalyAssistant;
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

    /// <summary>
    /// Reviews the most recent audit entries for unusual patterns. Tries the AI assistant
    /// first; if it's unavailable, falls back to a deterministic rule-based heuristic
    /// (activity bursts, off-hours actions) so the feature still returns real findings
    /// rather than an empty "unavailable" result.
    /// </summary>
    public async Task<Result<AuditAnomalyResponse>> AnalyzeAnomaliesAsync(CancellationToken cancellationToken)
    {
        var entries = await _auditLogRepository.SearchAsync(1, AnomalyWindowSize, cancellationToken);
        if (entries.Count == 0)
        {
            return Result<AuditAnomalyResponse>.Success(new AuditAnomalyResponse(Array.Empty<AuditAnomalyFinding>(), false, 0));
        }

        var aiFindings = await _anomalyAssistant.AnalyzeAsync(entries, cancellationToken);
        if (aiFindings is not null)
        {
            return Result<AuditAnomalyResponse>.Success(new AuditAnomalyResponse(aiFindings, true, entries.Count));
        }

        var heuristicFindings = RunHeuristics(entries);
        return Result<AuditAnomalyResponse>.Success(new AuditAnomalyResponse(heuristicFindings, false, entries.Count));
    }

    /// <summary>
    /// No-AI baseline: flags a user with a burst of 5+ actions inside any 10-minute window,
    /// and actions that occurred outside 06:00–21:00 UTC. Deliberately simple thresholds —
    /// this exists to give the feature real value even with no API key configured, not to
    /// replace genuine security monitoring.
    /// </summary>
    private static IReadOnlyList<AuditAnomalyFinding> RunHeuristics(IReadOnlyList<AuditLogEntryDto> entries)
    {
        var findings = new List<AuditAnomalyFinding>();
        var ordered = entries.OrderBy(e => e.OccurredOnUtc).ToArray();

        var burstUsers = new List<(string User, int Count, DateTime WindowStart)>();
        foreach (var user in ordered.Select(e => e.UtilisateurEmail).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct())
        {
            var userEntries = ordered.Where(e => e.UtilisateurEmail == user).ToArray();
            for (var i = 0; i < userEntries.Length; i++)
            {
                var windowEnd = userEntries[i].OccurredOnUtc.AddMinutes(10);
                var countInWindow = userEntries.Count(e => e.OccurredOnUtc >= userEntries[i].OccurredOnUtc && e.OccurredOnUtc <= windowEnd);
                if (countInWindow >= 5)
                {
                    burstUsers.Add((user!, countInWindow, userEntries[i].OccurredOnUtc));
                    break;
                }
            }
        }

        foreach (var burst in burstUsers.Take(5))
        {
            findings.Add(new AuditAnomalyFinding(
                "warning",
                $"Rafale d'actions — {burst.User}",
                $"{burst.Count} actions enregistrées en moins de 10 minutes à partir de {burst.WindowStart:dd/MM/yyyy HH:mm} UTC."));
        }

        var offHours = ordered.Where(e => e.OccurredOnUtc.Hour is < 6 or >= 21).ToArray();
        if (offHours.Length > 0)
        {
            findings.Add(new AuditAnomalyFinding(
                "info",
                "Actions en dehors des heures habituelles",
                $"{offHours.Length} action(s) enregistrée(s) entre 21h00 et 06h00 UTC sur la période analysée."));
        }

        if (findings.Count == 0)
        {
            findings.Add(new AuditAnomalyFinding(
                "info",
                "Aucune anomalie détectée",
                "Aucune rafale d'activité ni action hors horaires habituels sur la période analysée."));
        }

        return findings;
    }
}
