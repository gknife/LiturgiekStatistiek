namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// Translation-specific display name for a Bible book (e.g. "Richteren" vs
/// "Rechters"). Keyed by the translation abbreviation (HSV, SV, NBV21, NBG).
/// </summary>
public class BibleBookTranslationName
{
    public Guid Id { get; set; }
    public Guid BibleBookId { get; set; }
    public BibleBook BibleBook { get; set; } = null!;

    public string TranslationAbbreviation { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
