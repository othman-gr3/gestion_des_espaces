namespace GestionEspaces.Application.DTOs.Assignments;

/// <summary>
/// Represents a poste assignment.
/// </summary>
public sealed record AffectationPosteDto(
    int IdAffectationPoste,
    int AgentId,
    int BureauId,
    DateTime DateAffectation,
    DateTime? DateFin,
    string? Motif);