namespace GestionEspaces.Application.DTOs.Assignments;

/// <summary>
/// Request payload for closing (clôturer) an active affectation.
/// </summary>
public sealed record CloseAffectationRequest(DateTime DateFin);
