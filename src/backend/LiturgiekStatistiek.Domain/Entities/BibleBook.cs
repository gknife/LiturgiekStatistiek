namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// A book of the Protestant canon (66 books). Holds the canonical Dutch name and
/// a versification table (verse count per chapter) used to populate the chapter
/// and verse dropdowns. Per-translation spelling differences live in
/// <see cref="BibleBookTranslationName"/>.
/// </summary>
public class BibleBook
{
    public Guid Id { get; set; }

    /// <summary>1-based canonical order (Genesis = 1 ... Openbaring = 66).</summary>
    public int Ordinal { get; set; }

    /// <summary>"OT" or "NT".</summary>
    public string Testament { get; set; } = string.Empty;

    /// <summary>Canonical Dutch name (default display).</summary>
    public string Name { get; set; } = string.Empty;

    public int ChapterCount { get; set; }

    /// <summary>JSON array of verse counts per chapter, length == <see cref="ChapterCount"/>.</summary>
    public string VerseCountsJson { get; set; } = "[]";

    public ICollection<BibleBookTranslationName> TranslationNames { get; set; } = new List<BibleBookTranslationName>();
}
