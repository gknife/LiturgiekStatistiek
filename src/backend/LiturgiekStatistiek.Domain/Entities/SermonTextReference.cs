namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// A single normalized sermon-text passage for a service. Stored so queries can
/// filter on book and chapter. Cross-chapter or non-standard references that do
/// not fit the structured fields are preserved on the parent service's raw
/// <see cref="Service.SermonText"/> string.
/// </summary>
public class SermonTextReference
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public int Position { get; set; }

    /// <summary>Canonical book when recognised; null for fully custom references.</summary>
    public Guid? BibleBookId { get; set; }
    public BibleBook? BibleBook { get; set; }

    /// <summary>Book name as entered/displayed (snapshot; also set for custom references).</summary>
    public string BookName { get; set; } = string.Empty;

    public int? Chapter { get; set; }
    public int? VerseStart { get; set; }
    public int? VerseEnd { get; set; }
}
