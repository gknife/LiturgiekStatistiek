using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class LlmService : ILlmService
{
    private readonly ChatClient? _chatClient;
    private readonly ILogger<LlmService> _logger;
    private readonly bool _isConfigured;

    public LlmService(IConfiguration configuration, ILogger<LlmService> logger)
    {
        _logger = logger;
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var deployment = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
        {
            var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
            _chatClient = client.GetChatClient(deployment);
            _isConfigured = true;
        }
    }

    public async Task<LlmQueryParseResult> ParseNaturalLanguageQueryAsync(string query, CancellationToken ct = default)
    {
        if (!_isConfigured)
        {
            return new LlmQueryParseResult
            {
                Success = false,
                ErrorMessage = "Azure OpenAI is niet geconfigureerd. Gebruik de voorgedefinieerde sjablonen."
            };
        }

        var systemPrompt = @"Je bent een assistent voor het Liturgiek Statistiek platform. 
Je taak is om een vraag in het Nederlands te vertalen naar een van de volgende query-sjablonen:

Beschikbare sjablonen:
1. most-sung-song: Meest gezongen lied. Parameters: congregationId (verplicht), fromDate, toDate
2. most-sung-verse: Meest gezongen couplet. Parameters: year (verplicht), bundleId
3. most-opening-song: Meest gezongen openingslied. Parameters: fromDate, toDate
4. average-songs-per-service: Gemiddeld aantal liederen. Parameters: congregationId, fromDate, toDate
5. most-psalms-congregation: Meeste psalmen. Parameters: fromDate, toDate
6. song-by-city-map: Lied per stad. Parameters: bundleId (verplicht), songNumber (verplicht), fromDate, toDate
7. song-by-period: Lied per periode. Parameters: year (verplicht), month
8. services-with-song: Diensten met lied. Parameters: bundleId (verplicht), songNumber (verplicht)
9. song-after-song: Lied na lied. Parameters: bundleIdA, songNumberA, bundleIdB, songNumberB (allen verplicht)
10. song-usage-over-time: Gebruik over tijd. Parameters: bundleId (verplicht), songNumber (verplicht)

Bundel-afkortingen: Ps1773, PsOB, LvdK, WK, WKPs, Opw, GK, EG
Kerkgenootschappen: PKN, NGK, GG, GB, CGK, HHK

Antwoord ALLEEN met JSON in dit formaat:
{""templateId"": ""..., ""parameters"": {""key"": ""value""}}

Als de vraag niet past bij een sjabloon, antwoord met:
{""error"": ""Reden waarom de vraag niet verwerkt kan worden""}";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(query)
            };

            var response = await _chatClient!.CompleteChatAsync(messages, cancellationToken: ct);
            var content = response.Value.Content[0].Text;

            // Parse JSON response
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var errorProp))
            {
                return new LlmQueryParseResult { Success = false, ErrorMessage = errorProp.GetString() };
            }

            var templateId = root.GetProperty("templateId").GetString();
            var parameters = new Dictionary<string, string>();
            if (root.TryGetProperty("parameters", out var paramsProp))
            {
                foreach (var prop in paramsProp.EnumerateObject())
                {
                    parameters[prop.Name] = prop.Value.GetString() ?? "";
                }
            }

            return new LlmQueryParseResult { Success = true, TemplateId = templateId, Parameters = parameters };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse NL query: {Query}", query);
            return new LlmQueryParseResult
            {
                Success = false,
                ErrorMessage = $"Fout bij verwerking: {ex.Message}"
            };
        }
    }

    public async Task<LlmLiturgyParseResult> ParseLiturgyTextAsync(string text, CancellationToken ct = default)
    {
        if (!_isConfigured)
        {
            return new LlmLiturgyParseResult
            {
                Success = false,
                ErrorMessage = "Azure OpenAI is niet geconfigureerd."
            };
        }

        var systemPrompt = @"Je bent een assistent die liturgieteksten van Nederlandse kerkdiensten vertaalt naar gestructureerde JSON.

Bekende liedbundels en afkortingen:
- Ps1773: Psalmen 1773 (""Ps."" of ""Psalm"")
- PsOB: Psalmen Onberijmd
- LvdK: Liedboek voor de Kerken 
- WK: Weerklank
- WKPs: Weerklank Psalmen
- Opw: Opwekking (""Opwekking"")
- GK: Gereformeerd Kerkboek
- EG: Evangelische Gezangen

Antwoord ALLEEN met JSON in dit formaat:
{
  ""city"": ""...'',
  ""congregation"": ""..."",
  ""denomination"": ""..."",
  ""date"": ""YYYY-MM-DD"",
  ""timeOfDay"": ""ochtend|middag|avond"",
  ""preacher"": ""..."",
  ""bibleTranslation"": ""HSV|SV|NBV21|NBG"",
  ""sermonText"": ""..."",
  ""sermonTheme"": ""..."",
  ""broadcastUrl"": ""..."",
  ""elements"": [
    { ""position"": 1, ""label"": ""Voorzang"", ""songBundle"": ""Ps1773"", ""songNumber"": 63, ""verses"": [""2""], ""notes"": null },
    ...
  ]
}

Probeer alle informatie te herkennen. Bij twijfel, gebruik null.";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(text)
            };

            var response = await _chatClient!.CompleteChatAsync(messages, cancellationToken: ct);
            var content = response.Value.Content[0].Text;

            // Remove possible markdown code fences
            content = content.Trim();
            if (content.StartsWith("```"))
            {
                content = content.Split('\n', 2)[1];
                content = content[..content.LastIndexOf("```")];
            }

            var data = JsonSerializer.Deserialize<ParsedServiceData>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new LlmLiturgyParseResult { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse liturgy text");
            return new LlmLiturgyParseResult
            {
                Success = false,
                ErrorMessage = $"Fout bij verwerking: {ex.Message}"
            };
        }
    }
}
