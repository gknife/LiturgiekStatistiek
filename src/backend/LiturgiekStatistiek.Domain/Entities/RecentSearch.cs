namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// A natural-language search question entered by a user on the search page.
/// Kept per user so the most recent questions can be shown and re-run.
/// </summary>
public class RecentSearch
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string QueryText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
