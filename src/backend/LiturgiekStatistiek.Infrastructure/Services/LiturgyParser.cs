using System.Globalization;
using System.Text.RegularExpressions;
using LiturgiekStatistiek.Application.Interfaces;

namespace LiturgiekStatistiek.Infrastructure.Services;

/// <summary>
/// Deterministic liturgy parser. Recognises the 33 liturgical labels by keyword,
/// parses song references (bundle + number + verses), pulls out Schriftlezing and
/// Preektekst, and derives metadata from an optional title line. Unlabeled song
/// lines fall back to "Overig"; the first/last songs are promoted to
/// Openingslied/Slotlied as a light heuristic.
/// </summary>
public class LiturgyParser : ILiturgyParser
{
    private static readonly Dictionary<string, int> DutchMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["januari"] = 1, ["februari"] = 2, ["maart"] = 3, ["april"] = 4,
        ["mei"] = 5, ["juni"] = 6, ["juli"] = 7, ["augustus"] = 8,
        ["september"] = 9, ["oktober"] = 10, ["november"] = 11, ["december"] = 12,
    };

    // Ordered keyword -> label rules. First match wins, so more specific rules go first.
    private static readonly (string Keyword, string Label)[] LabelRules =
    {
        ("muziek voor de dienst (orgel)", "Muziek voor de dienst (orgel)"),
        ("muziek voor de dienst", "Muziek voor de dienst (anders)"),
        ("gebed om de opening", "Gebed om de opening van het Woord"),
        ("dankgebed", "Dankgebed"),
        ("dankzegging", "Dankgebed (met voorbeden/dankzegging)"),
        ("votum", "Votum"),
        ("groet", "Groet"),
        ("mededeling", "Mededelingen"),
        ("schriftlezing", "Schriftlezing(en)"),
        ("lezing", "Schriftlezing(en)"),
        ("vermaan", "Vermaan/belijden"),
        ("belijdenis", "Vermaan/belijden"),
        ("belijden", "Vermaan/belijden"),
        ("thema", "Thema preek"),
        ("preektekst", "Preektekst"),
        ("tekst", "Preektekst"),
        ("voorzang", "Voorzang"),
        ("openingslied", "Openingslied"),
        ("aanvangslied", "Openingslied"),
        ("intochtslied", "Openingslied"),
        ("kindermoment", "Bij kindermoment"),
        ("kinderlied", "Bij kindermoment"),
        ("voor de preek", "Voor de preek (zonder collecte)"),
        ("tussenzang", "Tussenzang"),
        ("na de preek", "Na de preek"),
        ("slotlied", "Slotlied"),
        ("zegenbede", "Zegenbede"),
        ("zegen", "Zegen"),
        ("collecte", "Collecte aan de deur"),
        ("gebed", "Grote gebed (met voorbeden/dankzegging)"),
    };

    private static readonly Dictionary<string, string> BundleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ps"] = "Ps1773", ["ps."] = "Ps1773", ["psalm"] = "Ps1773", ["psalmen"] = "Ps1773",
        ["gez"] = "Ps1773", ["gez."] = "Ps1773", ["gezang"] = "Ps1773",
        ["lvdk"] = "LvdK", ["lied"] = "LvdK", ["liedboek"] = "LvdK",
        ["opw"] = "Opw", ["opw."] = "Opw", ["opwekking"] = "Opw",
        ["wk"] = "WK", ["weerklank"] = "WK",
        ["wkps"] = "WKPs",
        ["gk"] = "GK",
        ["eg"] = "EG",
    };

    private static readonly Regex SongRegex = new(
        @"^(?<bundle>Ps\.?|Psalm(?:en)?|Gez\.?|Gezang|LvdK|Liedboek|Lied|Opw\.?|Opwekking|WKPs|WK|Weerklank|GK|EG)\s*(?<num>\d+)\s*[:.]?\s*(?<verses>[\d,\s\-]*(?:\s*en\s*\d+)*)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ParsedServiceData Parse(string text, string? title = null)
    {
        var data = ParseTitle(title);
        var elements = new List<ParsedElement>();
        string? sermonText = null;
        int position = 1;

        foreach (var rawSegment in SplitSegments(text))
        {
            var segment = NormalizeWhitespace(rawSegment);
            if (segment.Length == 0) continue;

            // Split "Label : value" form.
            string? labelPart = null;
            string valuePart = segment;
            var colon = segment.IndexOf(':');
            if (colon > 0 && colon < segment.Length - 1)
            {
                var before = segment[..colon].Trim();
                // Only treat as "Label : value" when the part before the colon is text (not a song number).
                if (before.Length > 0 && !char.IsDigit(before[^1]) && DetectLabel(before) != null)
                {
                    labelPart = before;
                    valuePart = segment[(colon + 1)..].Trim();
                }
            }

            var detectedLabel = labelPart != null ? DetectLabel(labelPart) : DetectLabel(segment);

            // Sermon text capture.
            if (detectedLabel == "Preektekst")
            {
                sermonText = valuePart.Length > 0 ? valuePart : segment;
                elements.Add(new ParsedElement { Position = position++, Label = "Preektekst", Notes = sermonText });
                continue;
            }

            // Scripture reading.
            if (detectedLabel == "Schriftlezing(en)")
            {
                elements.Add(new ParsedElement { Position = position++, Label = "Schriftlezing(en)", Notes = valuePart });
                continue;
            }

            // Song reference (possibly after a label).
            var song = TryParseSong(valuePart) ?? TryParseSong(segment);
            if (song != null)
            {
                elements.Add(new ParsedElement
                {
                    Position = position++,
                    Label = detectedLabel ?? "Overig",
                    SongBundle = song.Value.Bundle,
                    SongNumber = song.Value.Number,
                    Verses = song.Value.Verses.Count > 0 ? song.Value.Verses : null,
                });
                continue;
            }

            // Non-song labeled line.
            if (detectedLabel != null)
            {
                elements.Add(new ParsedElement
                {
                    Position = position++,
                    Label = detectedLabel,
                    Notes = labelPart != null && valuePart.Length > 0 ? valuePart : null,
                });
            }
        }

        ApplyFirstLastHeuristics(elements);

        return data with
        {
            SermonText = sermonText ?? data.SermonText,
            Elements = elements,
        };
    }

    private static void ApplyFirstLastHeuristics(List<ParsedElement> elements)
    {
        var songIndexes = new List<int>();
        for (var i = 0; i < elements.Count; i++)
        {
            if (elements[i].SongNumber != null) songIndexes.Add(i);
        }
        if (songIndexes.Count == 0) return;

        var firstIdx = songIndexes[0];
        if (elements[firstIdx].Label == "Overig")
        {
            elements[firstIdx] = elements[firstIdx] with { Label = "Openingslied" };
        }

        var lastIdx = songIndexes[^1];
        if (lastIdx != firstIdx && elements[lastIdx].Label == "Overig")
        {
            elements[lastIdx] = elements[lastIdx] with { Label = "Slotlied" };
        }
    }

    private static (string Bundle, int Number, List<string> Verses)? TryParseSong(string input)
    {
        var m = SongRegex.Match(input.Trim());
        if (!m.Success) return null;

        var bundleRaw = m.Groups["bundle"].Value.Trim().TrimEnd('.').ToLowerInvariant();
        if (!BundleMap.TryGetValue(bundleRaw, out var bundle) &&
            !BundleMap.TryGetValue(bundleRaw + ".", out bundle))
        {
            bundle = bundleRaw.ToUpperInvariant();
        }

        if (!int.TryParse(m.Groups["num"].Value, out var number)) return null;

        var verses = ParseVerses(m.Groups["verses"].Value);
        return (bundle, number, verses);
    }

    private static List<string> ParseVerses(string raw)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return result;

        var normalized = Regex.Replace(raw, @"\s+en\s+", ",", RegexOptions.IgnoreCase);
        foreach (var part in normalized.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var token = part.Trim();
            if (token.Length == 0) continue;

            // Expand ranges like "1-3".
            var range = Regex.Match(token, @"^(\d+)\s*-\s*(\d+)$");
            if (range.Success &&
                int.TryParse(range.Groups[1].Value, out var start) &&
                int.TryParse(range.Groups[2].Value, out var end) &&
                end >= start && end - start < 50)
            {
                for (var v = start; v <= end; v++) result.Add(v.ToString());
            }
            else if (Regex.IsMatch(token, @"^\d+$"))
            {
                result.Add(token);
            }
        }
        return result;
    }

    private static string? DetectLabel(string text)
    {
        var lower = text.ToLowerInvariant();
        foreach (var (keyword, label) in LabelRules)
        {
            if (lower.Contains(keyword)) return label;
        }
        return null;
    }

    private static string NormalizeWhitespace(string text) =>
        Regex.Replace(text, @"\s+", " ").Trim();

    private static IEnumerable<string> SplitSegments(string text)
    {
        var lines = text.Replace("\r", "\n").Split('\n');
        foreach (var line in lines)
        {
            foreach (var seg in line.Split(new[] { " / ", " | ", ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return seg;
            }
        }
    }

    private static ParsedServiceData ParseTitle(string? title)
    {
        var data = new ParsedServiceData();
        if (string.IsNullOrWhiteSpace(title)) return data;

        var parts = title.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? preacher = null, congregation = null, date = null, timeOfDay = null;

        foreach (var part in parts)
        {
            if (Regex.IsMatch(part, @"^(ds\.?|drs\.?|dr\.?|prof\.?|prop\.?|kand\.?|ds |dhr\.?)", RegexOptions.IgnoreCase) && preacher == null)
            {
                preacher = part;
                continue;
            }

            var dm = Regex.Match(part, @"(\d{1,2})\s+([A-Za-z]+)", RegexOptions.IgnoreCase);
            var tm = Regex.Match(part, @"(\d{1,2}):(\d{2})");
            if (dm.Success && DutchMonths.TryGetValue(dm.Groups[2].Value, out var month))
            {
                var day = int.Parse(dm.Groups[1].Value);
                var year = DateTime.UtcNow.Year;
                date = new DateTime(year, month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            if (tm.Success)
            {
                var hour = int.Parse(tm.Groups[1].Value);
                timeOfDay = hour < 12 ? "Morning" : hour < 17 ? "Afternoon" : "Evening";
                continue;
            }

            if (dm.Success) continue;

            // Remaining free text segment -> congregation/church name.
            if (preacher != null && congregation == null)
            {
                congregation = part;
            }
        }

        return data with
        {
            Preacher = preacher,
            Congregation = congregation,
            Date = date,
            TimeOfDay = timeOfDay,
        };
    }
}
