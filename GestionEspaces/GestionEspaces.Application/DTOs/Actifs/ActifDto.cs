using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Actifs;

public sealed record ActifDto(
    int IdActif,
    string? ConcurrencyToken,
    string Nom,
    string? Type,
    string? Marque,
    string? Modele,
    string? NumeroSerie,
    DateTime? DateAchat,
    EtatActif Etat,
    string? Image);