using GestionEspaces.Application.DTOs.AgentChat;

namespace GestionEspaces.Application.Interfaces;

/// <summary>
/// Answers an Agent's free-text question using only the self-service data already fetched
/// for them (their office, assets, history) — never anything else. Implemented against
/// OpenRouter in Infrastructure. Returns null on any failure so the caller can fall back to
/// a deterministic, keyword-based answer built from the same data.
/// </summary>
public interface IAgentChatAssistant
{
    Task<string?> AnswerAsync(string question, AgentChatContext context, CancellationToken cancellationToken);
}
