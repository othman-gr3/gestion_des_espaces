using GestionEspaces.Application.DTOs.Actifs;
using GestionEspaces.Application.DTOs.Bureaux;
using GestionEspaces.Application.DTOs.SelfService;

namespace GestionEspaces.Application.DTOs.AgentChat;

public sealed record AgentChatRequest(string Message);

public sealed record AgentChatResponse(string Answer, bool UsedAi);

/// <summary>
/// The agent's own self-service data, gathered before asking the assistant so it can only
/// ever answer from real, already-authorized data — never fabricate or reach into another
/// agent's records.
/// </summary>
public sealed record AgentChatContext(
    string NomComplet,
    BureauDto? Office,
    IReadOnlyList<ActifDto> Assets,
    MyHistoryResponse History);
