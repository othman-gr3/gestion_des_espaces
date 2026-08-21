namespace GestionEspaces.Application.DTOs.Audit;

public sealed record AuditLogEntryDto(
    int IdAuditLog,
    DateTime OccurredOnUtc,
    string EventType,
    string Payload,
    string? UtilisateurEmail,
    string? UtilisateurRole);
