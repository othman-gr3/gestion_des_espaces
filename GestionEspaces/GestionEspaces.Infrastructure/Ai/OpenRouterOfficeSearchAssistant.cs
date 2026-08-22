using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEspaces.Application.DTOs.OfficeSearchAi;
using GestionEspaces.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionEspaces.Infrastructure.Ai;

/// <summary>
/// Calls OpenRouter's OpenAI-compatible chat completions API to turn a French
/// natural-language office request into structured search criteria. Every failure mode
/// (unconfigured key, network error, non-2xx response, unparseable JSON) is caught and
/// returns null rather than throwing, so the caller can fall back to keyword search.
/// </summary>
public sealed class OpenRouterOfficeSearchAssistant : IOfficeSearchAssistant
{
    private const string PlaceholderApiKey = "__SET_VIA_ENV_GestionEspaces__OpenRouter__ApiKey__OR_USER_SECRETS__";
    private const string ChatCompletionsUrl = "https://openrouter.ai/api/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterOfficeSearchAssistant> _logger;

    public OpenRouterOfficeSearchAssistant(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouterOfficeSearchAssistant> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OfficeSearchCriteria?> InterpretAsync(string query, IReadOnlyList<BatimentOption> availableBatiments, CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("OpenRouter");
        var apiKey = section["ApiKey"];
        var model = string.IsNullOrWhiteSpace(section["Model"]) ? "openai/gpt-4o-mini" : section["Model"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == PlaceholderApiKey)
        {
            _logger.LogWarning("OpenRouter:ApiKey n'est pas configurée — recherche IA indisponible, repli sur la recherche par mot-clé.");
            return null;
        }

        try
        {
            var systemPrompt = BuildSystemPrompt(availableBatiments);

            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
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

            return new OfficeSearchCriteria(
                parsed.IdBatiment,
                parsed.Statut,
                parsed.Type,
                parsed.CapaciteMin,
                parsed.EtageMin,
                string.IsNullOrWhiteSpace(parsed.Summary) ? "Recherche effectuée." : parsed.Summary);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Échec de l'interprétation IA de la requête de recherche de bureau.");
            return null;
        }
    }

    private static string BuildSystemPrompt(IReadOnlyList<BatimentOption> availableBatiments)
    {
        var batimentsJson = JsonSerializer.Serialize(availableBatiments, JsonOptions);

        return
            "Tu es un assistant qui traduit une demande de bureau exprimée en français en critères de recherche " +
            "structurés. Réponds UNIQUEMENT avec un objet JSON valide, sans aucun texte autour, au format exact : " +
            "{\"idBatiment\": <int ou null>, \"statut\": <0=Disponible, 1=Occupé, 2=EnMaintenance, ou null>, " +
            "\"type\": <0=Individuel, 1=OpenSpace, 2=SalleReunion, ou null>, \"capaciteMin\": <int ou null>, " +
            "\"etageMin\": <int ou null>, \"summary\": \"<courte phrase en français résumant ce que tu as compris>\"}. " +
            "Mets statut à 0 (Disponible) par défaut si l'utilisateur ne précise rien, sauf s'il demande explicitement " +
            "autre chose (par exemple les bureaux en maintenance). " +
            $"Voici la liste des bâtiments existants, avec leur idBatiment et le site auquel ils appartiennent — " +
            $"utilise l'idBatiment correspondant si un nom de bâtiment ou de site est mentionné dans la demande : {batimentsJson}";
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

    private sealed class AiCriteriaPayload
    {
        public int? IdBatiment { get; set; }
        public int? Statut { get; set; }
        public int? Type { get; set; }
        public int? CapaciteMin { get; set; }
        public int? EtageMin { get; set; }
        public string? Summary { get; set; }
    }
}
