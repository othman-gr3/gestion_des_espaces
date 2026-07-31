namespace GestionEspaces.Application.DTOs.Sites;

public sealed record UpdateSiteRequest(
    string ConcurrencyToken,
    string Nom,
    string Code,
    string Rue,
    string Ville,
    string CodePostal,
    string Pays,
    string? Image);