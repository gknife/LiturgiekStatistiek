namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// A single normalized scripture passage read during a reading onderdeel
/// (schriftlezing). Mirrors <see cref="SermonTextReference"/> but is attached to a
/// <see cref="ServiceElement"/> so a reading can hold one or more structured parts.
/// The reading's Bible translation lives on the parent <see cref="ServiceElement"/>.
/// </summary>
public class ReadingReference
{
    public Guid Id { get; set; }
    public Guid ServiceElementId { get; set; }
    public ServiceElement ServiceElement { get; set; } = null!;

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
