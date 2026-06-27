using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IUserSettingsService
{
    /// <summary>Returns the user's stored settings JSON, or null if none saved yet.</summary>
    Task<UserSettingsDto?> GetForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Creates or updates the user's settings blob.</summary>
    Task<UserSettingsDto> UpsertAsync(string userId, UpdateUserSettingsRequest request, CancellationToken ct = default);
}
