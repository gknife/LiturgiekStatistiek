using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class Preacher : IHasAuditFields
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Guid? DenominationId { get; set; }
    public ListItem? Denomination { get; set; }

    /// <summary>Aanhef/titel (ListItem in the "Voorganger-titels" list), e.g. Ds., Dr., Prof.</summary>
    public Guid? TitleId { get; set; }
    public ListItem? Title { get; set; }

    public string? City { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<Service> Services { get; set; } = new List<Service>();

    /// <summary>Congregations this preacher is associated with (many-to-many).</summary>
    public ICollection<CongregationPreacher> Congregations { get; set; } = new List<CongregationPreacher>();
}
