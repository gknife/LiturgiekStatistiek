namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// Per-user application settings, stored as a JSON blob so new settings can be
/// added without a schema change. One row per user.
/// </summary>
public class UserSetting
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>JSON object holding the user's settings (theme, fontSize, accentColor, ...).</summary>
    public string SettingsJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; }
}
