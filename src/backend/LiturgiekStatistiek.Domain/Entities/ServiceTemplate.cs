using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// A reusable service-structure template variant. Selected by kerkgenootschap
/// (denomination), optionally overridden per gemeente (congregation), and matched
/// to a service by structured tags (time of day + occasion characteristic).
/// Instantiating a template pre-fills the onderdelen of a new service and acts as
/// the scaffold when parsing.
/// </summary>
public class ServiceTemplate : IHasAuditFields
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Kerkgenootschap this template belongs to (ListItem in the Denominations list).</summary>
    public Guid? DenominationId { get; set; }
    public ListItem? Denomination { get; set; }

    /// <summary>Optional per-gemeente override; when set this template only applies to that gemeente.</summary>
    public Guid? CongregationId { get; set; }
    public Congregation? Congregation { get; set; }

    /// <summary>Selector tag: time of day this variant is for; null = any.</summary>
    public TimeOfDay? TimeOfDay { get; set; }

    /// <summary>Selector tag: occasion characteristic (ListItem in the ServiceOccasion list); null = regulier/any.</summary>
    public Guid? OccasionId { get; set; }
    public ListItem? Occasion { get; set; }

    public bool IsActive { get; set; } = true;

    // --- Default service characteristics prefilled when instantiating ---

    /// <summary>Default muzikale begeleiding (ListItem in the MusicalAccompaniment list).</summary>
    public Guid? MusicalAccompanimentId { get; set; }
    public ListItem? MusicalAccompaniment { get; set; }

    /// <summary>Whether this template represents a leesdienst (no preacher present).</summary>
    public bool IsReadingService { get; set; }

    /// <summary>Beamer/liturgy presentation defaults.</summary>
    public bool HasBeamerLiturgy { get; set; }
    public bool HasBeamerTexts { get; set; }
    public bool HasBeamerSongs { get; set; }

    /// <summary>Default Bijbelvertaling (ListItem in the BibleTranslations list).</summary>
    public Guid? DefaultBibleTranslationId { get; set; }
    public ListItem? DefaultBibleTranslation { get; set; }

    /// <summary>Default liedbundel (ListItem in the SongBundles list) used to prefill lied onderdelen.</summary>
    public Guid? DefaultSongBundleId { get; set; }
    public ListItem? DefaultSongBundle { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<ServiceTemplateElement> Elements { get; set; } = new List<ServiceTemplateElement>();
}
