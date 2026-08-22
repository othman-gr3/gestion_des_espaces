using GestionEspaces.Application.DTOs.OfficeSearchAi;

namespace GestionEspaces.Application.Interfaces;

/// <summary>
/// Translates a free-text natural-language office request into structured Bureau search
/// criteria via an LLM. Implemented against OpenRouter in Infrastructure. Returns null on
/// any failure (unconfigured API key, network error, unparseable response) so callers can
/// degrade to a plain keyword search rather than failing the whole request.
/// </summary>
public interface IOfficeSearchAssistant
{
    Task<OfficeSearchCriteria?> InterpretAsync(string query, IReadOnlyList<BatimentOption> availableBatiments, CancellationToken cancellationToken);
}
