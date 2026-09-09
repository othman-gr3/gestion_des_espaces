using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.Application.DTOs.AgentChat;
using GestionEspaces.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionEspaces.Infrastructure.Ai;

/// <summary>
/// Calls OpenRouter's OpenAI-compatible chat completions API to answer an Agent's question
/// about their own workspace, grounded strictly in the self-service data already fetched for
/// them — the prompt instructs the model to answer only from that data and to say plainly
/// when it can't. Every failure mode is caught and returns null, so the caller can fall back
/// to a deterministic keyword-based answer.
/// </summary>
public sealed class OpenRouterAgentChatAssistant : IAgentChatAssistant
{
    private const string PlaceholderApiKey = "__SET_VIA_ENV_GestionEspaces__OpenRouter__ApiKey__OR_USER_SECRETS__";
    private const string ChatCompletionsUrl = "https://openrouter.ai/api/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterAgentChatAssistant> _logger;

    public OpenRouterAgentChatAssistant(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouterAgentChatAssistant> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> AnswerAsync(string question, AgentChatContext context, CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("OpenRouter");
        var apiKey = section["ApiKey"];
        var model = string.IsNullOrWhiteSpace(section["Model"]) ? "openai/gpt-4o-mini" : section["Model"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == PlaceholderApiKey)
        {
            _logger.LogWarning("OpenRouter:ApiKey n'est pas configurée — chatbot agent indisponible, repli sur la réponse par mot-clé.");
            return null;
        }

        try
        {
            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = BuildSystemPrompt(context) },
                    new { role = "user", content = question },
                },
                temperature = 0.2,
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl)
            {
                Content = JsonContent.Create(requestBody),
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Appel OpenRouter échoué avec le statut {StatusCode}.", httpResponse.StatusCode);
                return null;
            }

            var payload = await httpResponse.Content.ReadFromJsonAsync<OpenRouterResponse>(JsonOptions, cancellationToken);
            var content = payload?.Choices?.FirstOrDefault()?.Message?.Content;

            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Échec de la réponse IA du chatbot agent.");
            return null;
        }
    }

    private static string BuildSystemPrompt(AgentChatContext context)
    {
        var dataJson = JsonSerializer.Serialize(new
        {
            agent = context.NomComplet,
            bureauActuel = context.Office is null ? null : new { context.Office.Numero, context.Office.Etage, context.Office.Capacite },
            actifsConfies = context.Assets.Select(a => new { a.Nom, a.Type, a.NumeroSerie, Etat = a.Etat.ToString() }),
            nombreAffectationsPosteHistorique = context.History.Postes.Count,
            nombreAffectationsActifHistorique = context.History.Actifs.Count,
        }, JsonOptions);

        return
            $"Tu es l'assistant self-service de {context.NomComplet} dans l'application GestionEspaces (ONEE). " +
            "Réponds en français, en 1 à 3 phrases courtes, UNIQUEMENT à partir des données ci-dessous — " +
            "n'invente jamais une information absente. Si la question ne peut pas être répondue avec ces " +
            "données, dis-le clairement et propose de reformuler. Ne réponds jamais au sujet d'un autre agent. " +
            $"Données disponibles (JSON) : {dataJson}";
    }

    private sealed class OpenRouterResponse
    {
        public List<OpenRouterChoice>? Choices { get; set; }
    }

    private sealed class OpenRouterChoice
    {
        public OpenRouterMessage? Message { get; set; }
    }

    private sealed class OpenRouterMessage
    {
        public string? Content { get; set; }
    }
}
