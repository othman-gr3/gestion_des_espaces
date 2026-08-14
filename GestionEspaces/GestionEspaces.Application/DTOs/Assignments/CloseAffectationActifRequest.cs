using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Assignments;

/// <summary>
/// Request payload for closing (clôturer) an active actif affectation, optionally
/// recording the equipment's condition on return.
/// </summary>
public sealed record CloseAffectationActifRequest(DateTime DateFin, EtatActif? EtatRetour);
