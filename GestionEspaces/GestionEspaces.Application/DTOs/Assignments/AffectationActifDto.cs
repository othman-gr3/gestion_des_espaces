using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Assignments;

/// <summary>
/// Represents an asset assignment.
/// </summary>
public sealed record AffectationActifDto(
    int IdAffectationActif,
    int AgentId,
    int ActifId,
    DateTime DateAffectation,
    DateTime? DateFin,
    StatutAffectation Statut,
    EtatActif? EtatRetour);
