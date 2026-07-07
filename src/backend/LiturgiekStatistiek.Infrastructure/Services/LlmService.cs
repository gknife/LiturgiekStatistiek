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
    private readonly bool _hasEndpoint;
    private readonly bool _hasApiKey;
    private readonly string _deployment;

    public bool IsConfigured => _isConfigured;

    public LlmService(IConfiguration configuration, ILogger<LlmService> logger)
    {
        _logger = logger;
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        _deployment = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";

        _hasEndpoint = !string.IsNullOrWhiteSpace(endpoint);
        _hasApiKey = !string.IsNullOrWhiteSpace(apiKey);

        if (_hasEndpoint && _hasApiKey)
        {
            var credential = new ApiKeyCredential(apiKey!);
            // Azure AI Foundry exposes an OpenAI-compatible endpoint ending in "/openai/v1"
            // (host *.services.ai.azure.com). That endpoint does not use the classic Azure
            // "deployments/{name}" URL scheme, so AzureOpenAIClient would 404. Use the plain
            // OpenAIClient (which targets {endpoint}/chat/completions) for those endpoints and
            // the AzureOpenAIClient for classic "*.openai.azure.com" resources.
            var isOpenAiCompatible =
                endpoint!.Contains("/openai/v1", StringComparison.OrdinalIgnoreCase) ||
                endpoint.Contains(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase);

            if (isOpenAiCompatible)
            {
                var options = new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint) };
                var client = new OpenAI.OpenAIClient(credential, options);
                _chatClient = client.GetChatClient(_deployment);
            }
            else
            {
                var client = new AzureOpenAIClient(new Uri(endpoint), credential);
                _chatClient = client.GetChatClient(_deployment);
            }
            _isConfigured = true;
            _logger.LogInformation(
                "Azure OpenAI configured (deployment '{Deployment}'). Natural-language and parsing features are enabled.",
                _deployment);
        }
        else
        {
            var missing = new List<string>();
            if (!_hasEndpoint) missing.Add("AzureOpenAI:Endpoint");
            if (!_hasApiKey) missing.Add("AzureOpenAI:ApiKey");
            _logger.LogWarning(
                "Azure OpenAI is NOT configured. Missing/empty settings: {Missing}. " +
                "Deployment name in use: '{Deployment}'. Set these via user-secrets or environment variables, e.g.: " +
                "dotnet user-secrets set \"AzureOpenAI:Endpoint\" \"https://<resource>.openai.azure.com/\". " +
                "Natural-language queries and paste-to-parse will be unavailable until configured.",
                string.Join(", ", missing), _deployment);
        }
    }

    public LlmStatus GetStatus()
    {
        var message = _isConfigured
            ? $"Azure OpenAI is geconfigureerd (deployment '{_deployment}')."
            : BuildMissingMessage();

        return new LlmStatus
        {
            IsConfigured = _isConfigured,
            HasEndpoint = _hasEndpoint,
            HasApiKey = _hasApiKey,
            DeploymentName = _deployment,
            Message = message,
        };
    }

    private string BuildMissingMessage()
    {
        var missing = new List<string>();
        if (!_hasEndpoint) missing.Add("Endpoint");
        if (!_hasApiKey) missing.Add("ApiKey");
        return $"Azure OpenAI is niet geconfigureerd. Ontbrekend: {string.Join(", ", missing)}. " +
               "Stel deze in via user-secrets of omgevingsvariabelen.";
    }


    public async Task<LlmQueryParseResult> ParseNaturalLanguageQueryAsync(string query, CancellationToken ct = default)
    {
        if (!_isConfigured)
        {
            return new LlmQueryParseResult
            {
                Success = false,
                ErrorMessage = BuildMissingMessage() + " Gebruik de voorgedefinieerde sjablonen of de geavanceerde zoekfunctie."
            };
        }

        var systemPrompt = @"Je bent een assistent voor het Liturgiek Statistiek platform. 
Je taak is om een vraag in het Nederlands te vertalen naar een van de volgende query-sjablonen:

Beschikbare sjablonen:
1. most-sung-song: Meest gezongen lied in een gemeente OF kerkgenootschap. Parameters: congregationId (optioneel), denominationId (optioneel, bv ""GG"", ""PKN""), fromDate, toDate. Geef congregationId bij een gemeentenaam, denominationId bij een kerkgenootschap. Minstens één van beide is nodig.
2. most-sung-verse: Meest gezongen couplet. Parameters: year (optioneel; laat weg als geen jaar genoemd is), bundleId (optioneel), songNumber (optioneel; het liednummer, bv 119 voor Psalm 119)
3. most-opening-song: Meest gezongen openingslied. Parameters: fromDate, toDate
4. average-songs-per-service: Gemiddeld aantal liederen. Parameters: congregationId, fromDate, toDate
5. most-psalms-congregation: Meeste psalmen per gemeente. Parameters: fromDate, toDate
6. song-by-city-map: Lied per stad (op de kaart). Parameters: songNumber (verplicht), bundleId (optioneel; voor psalmen ""Ps1773""), fromDate, toDate. Gebruik dit ALTIJD bij vragen als ""welke stad zingt Psalm 150 het meest""; bundleId mag je op ""Ps1773"" zetten als er geen bundel genoemd is.
7. song-by-period: Lied per periode. Parameters: year (verplicht), month
8. services-with-song: Diensten met lied. Parameters: bundleId (verplicht), songNumber (verplicht)
9. song-after-song: Lied na lied. Parameters: bundleIdA, songNumberA, bundleIdB, songNumberB (allen verplicht)
10. song-usage-over-time: Gebruik over tijd. Parameters: bundleId (verplicht), songNumber (verplicht)
11. song-completeness: Wanneer wordt een lied volledig (alle coupletten) gezongen. Parameters: bundleId (verplicht), songNumber (optioneel; laat weg voor de hele bundel). Gebruik dit bij vragen als ""wanneer wordt Ps1773 93 volledig/helemaal/in zijn geheel gezongen"".
12. compare-denominations: Vergelijk lied-/psalmgebruik tussen kerkgenootschappen. Parameters: denominationIds (verplicht, kommagescheiden bv ""PKN,NGK""), fromDate, toDate. Gebruik dit bij vragen als ""vergelijk psalmengebruik tussen PKN en NGK"".

Bundel-afkortingen: Ps1773, PsOB, LvdK, WK, WKPs, Opw, GK, EG
Voor psalmen zonder expliciet genoemde bundel, gebruik bundleId ""Ps1773"".
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
                ErrorMessage = BuildMissingMessage()
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
