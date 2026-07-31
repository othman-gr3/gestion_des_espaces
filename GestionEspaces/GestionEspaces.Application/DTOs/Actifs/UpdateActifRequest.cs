using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Actifs;

public sealed record UpdateActifRequest(
    string ConcurrencyToken,
    string Nom,
    string? Type,
    string? Marque,
    string? Modele,
    string? NumeroSerie,
    DateTime? DateAchat,
    string? Image,
    EtatActif Etat);