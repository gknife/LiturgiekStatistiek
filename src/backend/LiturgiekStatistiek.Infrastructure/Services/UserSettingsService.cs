using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IApplicationDbContext _db;

    public UserSettingsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserSettingsDto?> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        var entity = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return entity == null ? null : new UserSettingsDto(entity.SettingsJson);
    }

    public async Task<UserSettingsDto> UpsertAsync(string userId, UpdateUserSettingsRequest request, CancellationToken ct = default)
    {
        var entity = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (entity == null)
        {
            entity = new UserSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SettingsJson = request.SettingsJson,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserSettings.Add(entity);
        }
        else
        {
            entity.SettingsJson = request.SettingsJson;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return new UserSettingsDto(entity.SettingsJson);
    }
}
