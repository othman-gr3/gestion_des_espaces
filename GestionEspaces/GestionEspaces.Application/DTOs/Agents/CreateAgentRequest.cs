namespace GestionEspaces.Application.DTOs.Agents;

/// <summary>
/// Request payload for creating an agent.
/// </summary>
public sealed record CreateAgentRequest(
    string Nom,
    string Prenom,
    string Matricule,
    string? Email,
    string? Telephone,
    string? Fonction,
    string? Departement,
    DateTime? DateEmbauche,
    string? Image);