using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class ServiceElement : IHasAuditFields
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int Position { get; set; }
    public ElementType ElementType { get; set; }
    public Guid? LabelId { get; set; }
    public ListItem? Label { get; set; }
    public string? ScriptureReference { get; set; }
    public string? Notes { get; set; }

    /// <summary>Who performs this (textual) onderdeel: ListItem in the ServicePerformer list. Optional.</summary>
    public Guid? PerformerId { get; set; }
    public ListItem? Performer { get; set; }

    /// <summary>Beurtzang (antiphonal/responsive singing) flag for song onderdelen.</summary>
    public bool IsBeurtzang { get; set; }

    /// <summary>Bible translation for a reading onderdeel (schriftlezing). Replaces the old service-wide translation.</summary>
    public Guid? BibleTranslationId { get; set; }
    public ListItem? BibleTranslation { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<ServiceElementSong> Songs { get; set; } = new List<ServiceElementSong>();
    public ICollection<ReadingReference> ReadingReferences { get; set; } = new List<ReadingReference>();
}
