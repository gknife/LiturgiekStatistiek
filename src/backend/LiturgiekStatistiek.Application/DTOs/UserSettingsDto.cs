namespace LiturgiekStatistiek.Application.DTOs;

public record UserSettingsDto(string SettingsJson);

public record UpdateUserSettingsRequest(string SettingsJson);
