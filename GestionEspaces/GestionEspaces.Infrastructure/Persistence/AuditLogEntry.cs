namespace GestionEspaces.Infrastructure.Persistence;

/// <summary>
/// A persisted record of a domain event. Lives in Infrastructure rather than Domain —
/// it is a technical/observability concern, not one of the business entities from the
/// cahier des charges. Deliberately carries no foreign keys to business entities so the
/// audit trail survives even after the record it describes is later deleted.
/// </summary>
public sealed class AuditLogEntry
{
    public int IdAuditLog { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? UtilisateurEmail { get; private set; }
    public string? UtilisateurRole { get; private set; }

    private AuditLogEntry()
    {
    }

    public AuditLogEntry(DateTime occurredOnUtc, string eventType, string payload, string? utilisateurEmail, string? utilisateurRole)
    {
        OccurredOnUtc = occurredOnUtc;
        EventType = eventType;
        Payload = payload;
        UtilisateurEmail = utilisateurEmail;
        UtilisateurRole = utilisateurRole;
    }
}
