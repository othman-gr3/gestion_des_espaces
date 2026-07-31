namespace GestionEspaces.Application.DTOs.Sites;

public sealed record SearchSitesRequest(
    string? SearchText,
    int PageNumber = 1,
    int PageSize = 20);