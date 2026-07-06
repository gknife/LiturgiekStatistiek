using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using LiturgiekStatistiek.Application.Interfaces;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class UrlImportService : IUrlImportService
{
    private static readonly HttpClient HttpClient = CreateClient();
    private readonly ILiturgyParser _parser;

    public UrlImportService(ILiturgyParser parser)
    {
        _parser = parser;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (LiturgiekStatistiek import)");
        return client;
    }

    public async Task<UrlImportResult> ImportAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new UrlImportResult { Success = false, ErrorMessage = "Ongeldige URL." };
        }

        string html;
        try
        {
            html = await HttpClient.GetStringAsync(uri, ct);
        }
        catch (Exception ex)
        {
            return new UrlImportResult { Success = false, ErrorMessage = $"Kon de pagina niet ophalen: {ex.Message}" };
        }

        var title = ExtractMeta(html, "og:title") ?? ExtractTitleTag(html);
        var description = ExtractMeta(html, "description") ?? ExtractMeta(html, "og:description");

        if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(title))
        {
            return new UrlImportResult { Success = false, ErrorMessage = "Geen liturgie gevonden op deze pagina." };
        }

        // kerkdienstgemist.nl is a SPA: the liturgy isn't in the static HTML, but the
        // church name and date/time-of-day sit in predictable places (og:title/url).
        // Use a dedicated extractor rather than the generic title heuristics, which
        // assume a different segment order.
        if (uri.Host.EndsWith("kerkdienstgemist.nl", StringComparison.OrdinalIgnoreCase))
        {
            var kdg = ParseKerkdienstgemist(uri, title, description) with { BroadcastUrl = url };
            return new UrlImportResult { Success = true, Data = kdg, RawDescription = description };
        }

        var data = _parser.Parse(description ?? string.Empty, title);
        data = data with { BroadcastUrl = url };

        return new UrlImportResult { Success = true, Data = data, RawDescription = description };
    }

    private static readonly Regex RecordingIdRegex = new(@"/recording/(?<id>\d{6,})", RegexOptions.Compiled);
    private static readonly Regex PreacherPrefix = new(
        @"^(ds\.?|drs\.?|dr\.?|prof\.?|prop\.?|kand\.?|dhr\.?|mw\.?|mevr\.?)\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScriptureRef = new(@"\d+\s*[:.]\s*\d", RegexOptions.Compiled);

    /// <summary>
    /// Extracts service metadata from a kerkdienstgemist.nl recording page. The
    /// recording id encodes the start time as <c>{unixSeconds}{stationId:D5}</c>, so
    /// the date and time-of-day come from there; the church name is the last
    /// <c>" - "</c> segment of the og:title, and the preacher/sermon text come from
    /// the og:title/description.
    /// </summary>
    public static ParsedServiceData ParseKerkdienstgemist(Uri uri, string? title, string? description)
    {
        string? date = null, timeOfDay = null;

        var idMatch = RecordingIdRegex.Match(uri.AbsolutePath);
        if (idMatch.Success)
        {
            var id = idMatch.Groups["id"].Value;
            // Strip the trailing 5-digit, zero-padded station id to recover the unix start time.
            if (id.Length > 5 && long.TryParse(id[..^5], out var unixSeconds) &&
                unixSeconds is > 946_684_800 and < 4_102_444_800) // 2000-01-01 .. 2100-01-01
            {
                var local = ToAmsterdam(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));
                date = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                timeOfDay = TimeOfDayFromHour(local.Hour);
            }
        }

        string? congregation = null, city = null, preacher = null, sermonTheme = null;

        var titleParts = (title ?? string.Empty)
            .Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (titleParts.Length > 0)
        {
            (congregation, city) = SplitChurchAndCity(titleParts[^1]);

            var first = titleParts[0];
            if (PreacherPrefix.IsMatch(first)) preacher = first;
            else timeOfDay ??= TimeOfDayFromServiceType(first);

            // With 3+ segments the middle segment is the sermon theme.
            if (titleParts.Length >= 3) sermonTheme = titleParts[^2];
        }

        string? sermonText = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            foreach (var part in description.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (preacher == null && PreacherPrefix.IsMatch(part)) { preacher = part; continue; }
                if (sermonText == null && ScriptureRef.IsMatch(part)) sermonText = part;
            }
        }

        return new ParsedServiceData
        {
            Date = date,
            TimeOfDay = timeOfDay,
            Congregation = congregation,
            City = city,
            Preacher = preacher,
            SermonTheme = sermonTheme,
            SermonText = sermonText,
        };
    }

    private static (string? Name, string? City) SplitChurchAndCity(string churchName)
    {
        var trimmed = NormalizeWhitespace(churchName);
        if (trimmed.Length == 0) return (null, null);

        var lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace <= 0) return (trimmed, null);

        // The last token is (almost) always the city; the rest is the congregation name.
        return (trimmed[..lastSpace].Trim(), trimmed[(lastSpace + 1)..].Trim());
    }

    private static string? TimeOfDayFromServiceType(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("avond")) return "Evening";
        if (lower.Contains("middag")) return "Afternoon";
        if (lower.Contains("morgen") || lower.Contains("ochtend") || lower.Contains("morning")) return "Morning";
        return null;
    }

    private static string TimeOfDayFromHour(int hour) =>
        hour < 12 ? "Morning" : hour < 17 ? "Afternoon" : "Evening";

    private static DateTimeOffset ToAmsterdam(DateTimeOffset utc)
    {
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(id));
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return utc.ToOffset(TimeSpan.FromHours(1)); // CET fallback; church services are daytime
    }

    private static string NormalizeWhitespace(string text) =>
        Regex.Replace(text, @"\s+", " ").Trim();

    private static string? ExtractMeta(string html, string name)
    {
        // property="og:title" or name="description", attribute order-independent.
        var patterns = new[]
        {
            $@"<meta[^>]+(?:name|property)\s*=\s*[""']{Regex.Escape(name)}[""'][^>]*content\s*=\s*[""'](?<v>[^""']*)[""']",
            $@"<meta[^>]+content\s*=\s*[""'](?<v>[^""']*)[""'][^>]*(?:name|property)\s*=\s*[""']{Regex.Escape(name)}[""']",
        };

        foreach (var pattern in patterns)
        {
            var m = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m.Success)
            {
                return WebUtility.HtmlDecode(m.Groups["v"].Value).Trim();
            }
        }
        return null;
    }

    private static string? ExtractTitleTag(string html)
    {
        var m = Regex.Match(html, @"<title[^>]*>(?<v>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return m.Success ? WebUtility.HtmlDecode(m.Groups["v"].Value).Trim() : null;
    }
}
