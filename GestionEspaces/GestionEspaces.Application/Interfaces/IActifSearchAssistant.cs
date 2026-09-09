using GestionEspaces.Application.DTOs.AiSearch;

namespace GestionEspaces.Application.Interfaces;

/// <summary>
/// Translates a free-text natural-language asset request into structured search criteria
/// (état + keyword) via an LLM. Implemented against OpenRouter in Infrastructure. Returns
/// null on any failure so callers can degrade to a plain keyword search.
/// </summary>
public interface IActifSearchAssistant
{
    Task<ActifSearchCriteria?> InterpretAsync(string query, CancellationToken cancellationToken);
}
