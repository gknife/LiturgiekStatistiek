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

        var data = _parser.Parse(description ?? string.Empty, title);
        data = data with { BroadcastUrl = url };

        return new UrlImportResult { Success = true, Data = data, RawDescription = description };
    }

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
