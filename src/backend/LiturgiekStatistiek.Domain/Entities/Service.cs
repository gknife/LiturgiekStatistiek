using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class Service : IHasAuditFields
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOfDay TimeOfDay { get; set; }
    public Guid CongregationId { get; set; }
    public Congregation Congregation { get; set; } = null!;
    public Guid? PreacherId { get; set; }
    public Preacher? Preacher { get; set; }
    public Guid? ChurchCalendarSundayId { get; set; }
    public ListItem? ChurchCalendarSunday { get; set; }
    public Guid? BibleTranslationId { get; set; }
    public ListItem? BibleTranslation { get; set; }
    public bool IsReadingService { get; set; }
    public string? ReadSermonBy { get; set; }
    public Guid? MusicalAccompanimentId { get; set; }
    public ListItem? MusicalAccompaniment { get; set; }
    public bool HasBeamerLiturgy { get; set; }
    public bool HasBeamerTexts { get; set; }
    public bool HasBeamerSongs { get; set; }
    public bool HasBeamerTextsAndSongs { get; set; }
    public string? BroadcastUrl { get; set; }
    public Guid? SpecialOccasionId { get; set; }
    public ListItem? SpecialOccasion { get; set; }
    public string? SermonText { get; set; }
    public string? SermonTheme { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<ServiceElement> Elements { get; set; } = new List<ServiceElement>();
    public ICollection<ServiceBundle> Bundles { get; set; } = new List<ServiceBundle>();
    public ICollection<ServiceMetadata> Metadata { get; set; } = new List<ServiceMetadata>();
}
