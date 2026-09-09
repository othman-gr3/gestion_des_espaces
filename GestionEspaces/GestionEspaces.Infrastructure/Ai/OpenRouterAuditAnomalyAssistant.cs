using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.Application.DTOs.Audit;
using GestionEspaces.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionEspaces.Infrastructure.Ai;

/// <summary>
/// Calls OpenRouter's OpenAI-compatible chat completions API to review recent audit log
/// entries for unusual patterns. Sends only timestamp/event type/user — never the raw
/// payload — to keep the prompt small and avoid forwarding more detail than needed. Every
/// failure mode is caught and returns null, so the caller can fall back to the rule-based
/// heuristic in <see cref="Application.UseCases.AuditLogUseCases"/>.
/// </summary>
public sealed class OpenRouterAuditAnomalyAssistant : IAuditAnomalyAssistant
{
    private const string PlaceholderApiKey = "__SET_VIA_ENV_GestionEspaces__OpenRouter__ApiKey__OR_USER_SECRETS__";
    private const string ChatCompletionsUrl = "https://openrouter.ai/api/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterAuditAnomalyAssistant> _logger;

    public OpenRouterAuditAnomalyAssistant(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouterAuditAnomalyAssistant> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AuditAnomalyFinding>?> AnalyzeAsync(IReadOnlyList<AuditLogEntryDto> entries, CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("OpenRouter");
        var apiKey = section["ApiKey"];
        var model = string.IsNullOrWhiteSpace(section["Model"]) ? "openai/gpt-4o-mini" : section["Model"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == PlaceholderApiKey)
        {
            _logger.LogWarning("OpenRouter:ApiKey n'est pas configurée — détection d'anomalies IA indisponible, repli sur l'heuristique.");
            return null;
        }

        try
        {
            var compactEntries = entries
                .Select(e => new { e.OccurredOnUtc, e.EventType, User = e.UtilisateurEmail ?? "système" })
                .ToArray();
            var entriesJson = JsonSerializer.Serialize(compactEntries, JsonOptions);

            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = entriesJson },
                },
                response_format = new { type = "json_object" },
                temperature = 0.1,
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
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Réponse OpenRouter sans contenu exploitable.");
                return null;
            }

            var parsed = JsonSerializer.Deserialize<AnomalyPayload>(content, JsonOptions);
            if (parsed?.Findings is null)
            {
                return null;
            }

            return parsed.Findings
                .Where(f => !string.IsNullOrWhiteSpace(f.Title))
                .Select(f => new AuditAnomalyFinding(
                    f.Severity is "warning" or "info" ? f.Severity : "info",
                    f.Title!,
                    f.Description ?? string.Empty))
                .Take(10)
                .ToArray();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Échec de l'analyse IA du journal d'audit.");
            return null;
        }
    }

    private const string SystemPrompt =
        "Tu es un assistant qui analyse un journal d'audit d'une application interne (affectations de bureaux " +
        "et de matériel) pour repérer des schémas inhabituels : rafales d'actions par un même utilisateur, " +
        "actions à des horaires atypiques, ou déséquilibre marqué entre utilisateurs. Ce journal ne contient " +
        "que des actions métier réussies (pas de tentatives de connexion échouées), donc reste factuel et " +
        "prudent — décris des schémas, n'accuse jamais explicitement quelqu'un de malveillance. Réponds " +
        "UNIQUEMENT avec un objet JSON valide, sans texte autour, au format exact : {\"findings\": " +
        "[{\"severity\": \"info\" ou \"warning\", \"title\": \"<titre court en français>\", \"description\": " +
        "\"<1-2 phrases en français>\"}]}. Si rien d'inhabituel ne ressort, réponds avec une liste findings " +
        "contenant un seul élément \"info\" qui le dit explicitement. Limite-toi à 5 éléments maximum.";

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

    private sealed class AnomalyPayload
    {
        public List<AnomalyFindingPayload>? Findings { get; set; }
    }

    private sealed class AnomalyFindingPayload
    {
        public string? Severity { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
