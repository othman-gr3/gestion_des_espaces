namespace GestionEspaces.Application.DTOs.Bureaux;

public sealed record CreateBureauRequest(
    string Numero,
    string? Type,
    int Capacite,
    float Superficie,
    int Etage,
    string? Image,
    int IdBatiment);