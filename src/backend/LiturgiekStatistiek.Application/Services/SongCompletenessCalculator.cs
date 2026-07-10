using System.Text.RegularExpressions;

namespace LiturgiekStatistiek.Application.Services;

/// <summary>
/// Pure logic for deciding whether a song was "sung as a whole". Completeness is
/// computed automatically by comparing the set of sung verse numbers against the
/// song's catalog verse count. Two states are distinguished and both are flagged:
/// complete within a single onderdeel, and complete across the whole service
/// (union of that song's verses over all onderdelen). When the catalog count is
/// unknown the result is "onbekend" and never counted as complete.
/// </summary>
public static class SongCompletenessCalculator
{
    public const string StateCompleteElement = "compleet-onderdeel";
    public const string StateCompleteService = "compleet-dienst";
    public const string StateIncomplete = "onvolledig";
    public const string StateUnknown = "onbekend";

    private static readonly Regex NumberToken = new(@"\d+", RegexOptions.Compiled);

    /// <summary>
    /// Parses the numeric verse numbers out of a set of verse labels. Non-numeric
    /// labels (refrains, "1a") are ignored for completeness. A range like "1-4"
    /// expands to 1,2,3,4.
    /// </summary>
    public static IReadOnlySet<int> ParseVerseNumbers(IEnumerable<string> verseLabels)
    {
        var result = new HashSet<int>();
        foreach (var label in verseLabels ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(label)) continue;

            var rangeMatch = Regex.Match(label, @"^\s*(\d+)\s*[-–]\s*(\d+)\s*$");
            if (rangeMatch.Success)
            {
                var start = int.Parse(rangeMatch.Groups[1].Value);
                var end = int.Parse(rangeMatch.Groups[2].Value);
                if (start <= end && end - start < 500)
                {
                    for (var v = start; v <= end; v++) result.Add(v);
                    continue;
                }
            }

            foreach (Match m in NumberToken.Matches(label))
            {
                if (int.TryParse(m.Value, out var n)) result.Add(n);
            }
        }
        return result;
    }

    /// <summary>
    /// Computes completeness. <paramref name="elementVerseLabels"/> are the verses in
    /// the single onderdeel; <paramref name="serviceVerseLabels"/> are all verse
    /// labels for the same song across the whole service (including this onderdeel).
    /// </summary>
    public static SongCompleteness Compute(
        int? catalogVerseCount,
        IEnumerable<string> elementVerseLabels,
        IEnumerable<string> serviceVerseLabels,
        bool sungInFull = false)
    {
        var elementVerses = ParseVerseNumbers(elementVerseLabels);
        var serviceVerses = ParseVerseNumbers(serviceVerseLabels);

        // An explicit "hele lied" marking always counts as complete, even when the
        // catalog verse count is unknown (many bundles have no per-song verse counts).
        if (sungInFull)
        {
            return new SongCompleteness(StateCompleteElement, true, true, catalogVerseCount, elementVerses.Count);
        }

        if (catalogVerseCount is null || catalogVerseCount.Value <= 0)
        {
            return new SongCompleteness(StateUnknown, false, false, catalogVerseCount, elementVerses.Count);
        }

        var full = Enumerable.Range(1, catalogVerseCount.Value).ToHashSet();
        var completeInElement = full.IsSubsetOf(elementVerses);
        var completeInService = full.IsSubsetOf(serviceVerses);

        var state = completeInElement
            ? StateCompleteElement
            : completeInService
                ? StateCompleteService
                : StateIncomplete;

        return new SongCompleteness(state, completeInElement, completeInService, catalogVerseCount, elementVerses.Count);
    }
}

public readonly record struct SongCompleteness(
    string State,
    bool CompleteInElement,
    bool CompleteInService,
    int? CatalogVerseCount,
    int SungVerseCount);
