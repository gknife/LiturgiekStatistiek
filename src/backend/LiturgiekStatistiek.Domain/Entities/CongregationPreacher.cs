namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many association between a congregation and its
/// associated pastors. At most one association per congregation should be marked
/// <see cref="IsPrimary"/>; the primary pastor is auto-filled when adding a service
/// for the congregation.
/// </summary>
public class CongregationPreacher
{
    public Guid CongregationId { get; set; }
    public Congregation Congregation { get; set; } = null!;

    public Guid PreacherId { get; set; }
    public Preacher Preacher { get; set; } = null!;

    public bool IsPrimary { get; set; }
}
