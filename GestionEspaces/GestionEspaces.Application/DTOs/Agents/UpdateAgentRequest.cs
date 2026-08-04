namespace GestionEspaces.Application.DTOs.Agents;

/// <summary>
/// Request payload for updating an agent.
/// </summary>
public sealed record UpdateAgentRequest(
    string ConcurrencyToken,
    string Nom,
    string Prenom,
    string Matricule,
    string? Email,
    string? Telephone,
    string? Fonction,
    string? Departement,
    DateTime? DateEmbauche,
    string? Image);
