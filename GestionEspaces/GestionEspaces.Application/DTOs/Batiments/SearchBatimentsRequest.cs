namespace GestionEspaces.Application.DTOs.Batiments;

public sealed record SearchBatimentsRequest(
    int? IdSite,
    string? SearchText,
    int PageNumber = 1,
    int PageSize = 20);