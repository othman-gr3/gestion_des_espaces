namespace GestionEspaces.Application.DTOs.Agents;

/// <summary>
/// Represents an agent in API responses.
/// </summary>
public sealed record AgentDto(
    int IdAgent,
    string Nom,
    string Prenom,
    string Matricule,
    string? Email,
    string? Telephone,
    string? Fonction,
    string? Departement,
    DateTime? DateEmbauche,
    string? Image);