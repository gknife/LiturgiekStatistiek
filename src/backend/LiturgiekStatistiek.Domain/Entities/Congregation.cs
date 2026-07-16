using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class Congregation : IHasAuditFields
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? LocationDetail { get; set; }
    public Guid? DenominationId { get; set; }
    public ListItem? Denomination { get; set; }
    public string? Modality { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<Service> Services { get; set; } = new List<Service>();

    /// <summary>Preachers associated with this congregation (many-to-many); one may be marked primary.</summary>
    public ICollection<CongregationPreacher> Pastors { get; set; } = new List<CongregationPreacher>();
}
