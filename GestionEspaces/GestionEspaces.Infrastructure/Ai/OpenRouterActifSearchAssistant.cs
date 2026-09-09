using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.Application.DTOs.AiSearch;
using GestionEspaces.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionEspaces.Infrastructure.Ai;

/// <summary>
/// Calls OpenRouter's OpenAI-compatible chat completions API to turn a French
/// natural-language asset request into structured search criteria (état + keyword). Every
/// failure mode is caught and returns null rather than throwing, so the caller can fall back
/// to keyword search.
/// </summary>
public sealed class OpenRouterActifSearchAssistant : IActifSearchAssistant
{
    private const string PlaceholderApiKey = "__SET_VIA_ENV_GestionEspaces__OpenRouter__ApiKey__OR_USER_SECRETS__";
    private const string ChatCompletionsUrl = "https://openrouter.ai/api/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterActifSearchAssistant> _logger;

    public OpenRouterActifSearchAssistant(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouterActifSearchAssistant> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ActifSearchCriteria?> InterpretAsync(string query, CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("OpenRouter");
        var apiKey = section["ApiKey"];
        var model = string.IsNullOrWhiteSpace(section["Model"]) ? "openai/gpt-4o-mini" : section["Model"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == PlaceholderApiKey)
        {
            _logger.LogWarning("OpenRouter:ApiKey n'est pas configurée — recherche IA d'actifs indisponible, repli sur la recherche par mot-clé.");
            return null;
        }

        try
        {
            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = query },
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

            var parsed = JsonSerializer.Deserialize<AiCriteriaPayload>(content, JsonOptions);
            if (parsed is null)
            {
                return null;
            }

            return new ActifSearchCriteria(
                parsed.Etat,
                parsed.Keyword,
                string.IsNullOrWhiteSpace(parsed.Summary) ? "Recherche effectuée." : parsed.Summary);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Échec de l'interprétation IA de la requête de recherche d'actif.");
            return null;
        }
    }

    private const string SystemPrompt =
        "Tu es un assistant qui traduit une demande de matériel (actif) exprimée en français en critères de " +
        "recherche structurés. Réponds UNIQUEMENT avec un objet JSON valide, sans aucun texte autour, au format " +
        "exact : {\"etat\": <0=Neuf, 1=BonEtat, 2=ARepairer, 3=HorsService, ou null>, \"keyword\": <chaîne courte " +
        "ou null>, \"summary\": \"<courte phrase en français résumant ce que tu as compris>\"}. Le mot-clé sera " +
        "comparé à la désignation, au type et au numéro de série de l'actif — choisis le terme le plus " +
        "discriminant de la demande (ex. \"ordinateur portable\", \"écran\"), ou null si aucun terme ne se " +
        "dégage. Laisse état à null si l'utilisateur ne précise rien sur la condition du matériel.";

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

    private sealed class AiCriteriaPayload
    {
        public int? Etat { get; set; }
        public string? Keyword { get; set; }
        public string? Summary { get; set; }
    }
}
