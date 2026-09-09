using GestionEspaces.Application.DTOs.AiSearch;

namespace GestionEspaces.Application.Interfaces;

/// <summary>
/// Translates a free-text natural-language agent request into a structured search keyword
/// via an LLM. Implemented against OpenRouter in Infrastructure. Returns null on any failure
/// so callers can degrade to a plain keyword search rather than failing the whole request.
/// </summary>
public interface IAgentSearchAssistant
{
    Task<AgentSearchCriteria?> InterpretAsync(string query, CancellationToken cancellationToken);
}
