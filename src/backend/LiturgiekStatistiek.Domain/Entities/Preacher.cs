using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class Preacher : IHasAuditFields
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public Guid? DenominationId { get; set; }
    public ListItem? Denomination { get; set; }
    public string? City { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<Service> Services { get; set; } = new List<Service>();
}
