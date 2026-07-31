namespace GestionEspaces.Application.DTOs.Actifs;

public sealed record CreateActifRequest(
    string Nom,
    string? Type,
    string? Marque,
    string? Modele,
    string? NumeroSerie,
    DateTime? DateAchat,
    string? Image);