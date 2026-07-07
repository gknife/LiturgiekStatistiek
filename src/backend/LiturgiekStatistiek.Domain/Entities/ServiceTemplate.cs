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

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<ServiceTemplateElement> Elements { get; set; } = new List<ServiceTemplateElement>();
}
