namespace LiturgiekStatistiek.Domain.Entities;

public class SongCatalogVerse
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Song Song { get; set; } = null!;
    public int Number { get; set; }
    public string? Title { get; set; }

    /// <summary>
    /// Optional name for a non-numbered verse (e.g. "Voorzang", "Tussenzang", "Slotzang").
    /// When set the verse is displayed by this label instead of its number and is excluded
    /// from the numbered-verse completeness count.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>Explicit display order; lets named verses sit before/between/after numbered verses.</summary>
    public int SortOrder { get; set; }
}
