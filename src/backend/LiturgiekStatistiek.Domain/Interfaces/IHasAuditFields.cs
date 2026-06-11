namespace LiturgiekStatistiek.Domain.Interfaces;

public interface IHasAuditFields
{
    string? CreatedBy { get; set; }
    DateTime CreatedAt { get; set; }
    string? ModifiedBy { get; set; }
    DateTime? ModifiedAt { get; set; }
}
