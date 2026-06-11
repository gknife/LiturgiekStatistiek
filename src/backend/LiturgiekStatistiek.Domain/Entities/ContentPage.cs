namespace LiturgiekStatistiek.Domain.Entities;

public class ContentPage
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string TitleNl { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
