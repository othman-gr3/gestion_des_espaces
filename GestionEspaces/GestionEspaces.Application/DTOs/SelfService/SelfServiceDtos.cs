using GestionEspaces.Application.DTOs.Actifs;
using GestionEspaces.Application.DTOs.Bureaux;

namespace GestionEspaces.Application.DTOs.SelfService;

public sealed record MyPosteHistoryDto(int IdAffectationPoste, BureauDto Bureau, DateTime DateAffectation, DateTime? DateFin, string? Motif, bool EstActive);

public sealed record MyActifHistoryDto(int IdAffectationActif, ActifDto Actif, DateTime DateAffectation, DateTime? DateFin, bool EstActive);

public sealed record MyHistoryResponse(IReadOnlyList<MyPosteHistoryDto> Postes, IReadOnlyList<MyActifHistoryDto> Actifs);

public sealed record UpdateMyProfileRequest(string ConcurrencyToken, string? Telephone);
