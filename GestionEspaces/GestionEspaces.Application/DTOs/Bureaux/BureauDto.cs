using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Bureaux;

public sealed record BureauDto(
    int IdBureau,
    string? ConcurrencyToken,
    string Numero,
    string? Type,
    int Capacite,
    float Superficie,
    int Etage,
    StatutBureau Statut,
    string? Image,
    int IdBatiment);