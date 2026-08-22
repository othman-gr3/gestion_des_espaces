using GestionEspaces.Application.DTOs.Bureaux;

namespace GestionEspaces.Application.DTOs.OfficeSearchAi;

/// <summary>A building the assistant can reference by id when a query names a site/bâtiment.</summary>
public sealed record BatimentOption(int IdBatiment, string Nom, string SiteNom);

/// <summary>Structured search criteria extracted from a natural-language query.</summary>
public sealed record OfficeSearchCriteria(
    int? IdBatiment,
    int? Statut,
    int? Type,
    int? CapaciteMin,
    int? EtageMin,
    string Summary);

public sealed record OfficeSearchAiRequest(string Query);

public sealed record OfficeSearchAiResponse(
    IReadOnlyCollection<BureauDto> Results,
    string Summary,
    bool UsedAi);
