using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.DTOs.Bureaux;

public sealed record CreateBureauRequest(
    string Numero,
    TypeBureau? Type,
    int Capacite,
    float Superficie,
    int Etage,
    string? Image,
    int IdBatiment);