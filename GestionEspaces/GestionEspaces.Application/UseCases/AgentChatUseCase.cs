using System.Text;
using GestionEspaces.Application.Common;
using GestionEspaces.Application.DTOs.AgentChat;
using GestionEspaces.Application.Interfaces;

namespace GestionEspaces.Application.UseCases;

/// <summary>
/// Orchestrates the Agent self-service chatbot: fetch the agent's own office/assets/history
/// through the existing self-service queries (so the assistant can never see or answer with
/// anyone else's data), then ask the assistant to phrase an answer grounded in that data. If
/// the assistant is unavailable, falls back to a small deterministic keyword-based answer
/// built from the same data, so the feature degrades rather than failing outright.
/// </summary>
public sealed class AgentChatUseCase
{
    private readonly IAgentChatAssistant _assistant;
    private readonly AgentSelfServiceUseCase _selfService;

    public AgentChatUseCase(IAgentChatAssistant assistant, AgentSelfServiceUseCase selfService)
    {
        _assistant = assistant;
        _selfService = selfService;
    }

    public async Task<Result<AgentChatResponse>> ExecuteAsync(string email, string? message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Result<AgentChatResponse>.Failure(new ErrorDetail("ValidationError", "Le message ne peut pas être vide.", "Message"));
        }

        var profileResult = await _selfService.GetMyProfileAsync(email, cancellationToken);
        if (!profileResult.IsSuccess)
        {
            return Result<AgentChatResponse>.Failure(profileResult.Errors);
        }

        var officeResult = await _selfService.GetMyOfficeAsync(email, cancellationToken);
        var assetsResult = await _selfService.GetMyAssetsAsync(email, cancellationToken);
        var historyResult = await _selfService.GetMyHistoryAsync(email, cancellationToken);

        var context = new AgentChatContext(
            $"{profileResult.Value!.Prenom} {profileResult.Value!.Nom}",
            officeResult.IsSuccess ? officeResult.Value : null,
            assetsResult.IsSuccess ? assetsResult.Value! : Array.Empty<Application.DTOs.Actifs.ActifDto>(),
            historyResult.IsSuccess
                ? historyResult.Value!
                : new DTOs.SelfService.MyHistoryResponse(Array.Empty<DTOs.SelfService.MyPosteHistoryDto>(), Array.Empty<DTOs.SelfService.MyActifHistoryDto>()));

        var answer = await _assistant.AnswerAsync(message, context, cancellationToken);
        if (answer is not null)
        {
            return Result<AgentChatResponse>.Success(new AgentChatResponse(answer, true));
        }

        return Result<AgentChatResponse>.Success(new AgentChatResponse(BuildFallbackAnswer(message, context), false));
    }

    /// <summary>
    /// Deterministic, keyword-based answer used when the AI assistant is unavailable —
    /// no LLM involved, but still genuinely answers from the agent's real data.
    /// </summary>
    private static string BuildFallbackAnswer(string message, AgentChatContext context)
    {
        var lower = message.ToLowerInvariant();

        if (lower.Contains("bureau") || lower.Contains("poste") || lower.Contains("où") || lower.Contains("ou suis"))
        {
            return context.Office is null
                ? "Vous n'avez actuellement aucun bureau qui vous est affecté."
                : $"Votre bureau actuel est le {context.Office.Numero} (étage {context.Office.Etage}, capacité {context.Office.Capacite}).";
        }

        if (lower.Contains("actif") || lower.Contains("matériel") || lower.Contains("materiel") || lower.Contains("équipement") || lower.Contains("equipement"))
        {
            if (context.Assets.Count == 0)
            {
                return "Aucun actif ne vous est actuellement confié.";
            }

            var list = string.Join(", ", context.Assets.Select(a => a.Nom));
            return $"Le matériel suivant vous est actuellement confié : {list}.";
        }

        if (lower.Contains("historique"))
        {
            var sb = new StringBuilder();
            sb.Append($"Vous avez eu {context.History.Postes.Count} affectation(s) de poste et {context.History.Actifs.Count} affectation(s) d'actif au total. ");
            sb.Append("Consultez la page « Mon historique » pour le détail.");
            return sb.ToString();
        }

        return "Assistant IA indisponible — je peux répondre par mot-clé à des questions contenant « bureau », « actif »/« matériel » ou « historique ». Reformulez votre question avec l'un de ces mots.";
    }
}
