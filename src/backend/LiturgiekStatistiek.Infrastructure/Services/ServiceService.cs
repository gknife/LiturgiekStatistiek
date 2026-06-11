using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class ServiceService : IServiceService
{
    private readonly ApplicationDbContext _context;

    public ServiceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ServiceSummaryDto>> GetServicesAsync(
        int page = 1,
        int pageSize = 20,
        Guid? congregationId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        var query = _context.Services
            .Include(s => s.Congregation)
            .Include(s => s.Preacher)
            .Include(s => s.SpecialOccasion)
            .AsQueryable();

        if (congregationId.HasValue)
        {
            query = query.Where(s => s.CongregationId == congregationId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.Date <= toDate.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.TimeOfDay)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ServiceSummaryDto(
                s.Id,
                s.Date,
                s.TimeOfDay.ToString(),
                s.Congregation.Name,
                s.Congregation.City,
                s.Preacher != null ? s.Preacher.FullName : null,
                s.SpecialOccasion != null ? s.SpecialOccasion.Value : null,
                s.Elements.Count))
            .ToListAsync();

        return new PaginatedResult<ServiceSummaryDto>(items, totalCount, page, pageSize);
    }

    public async Task<ServiceDto?> GetServiceByIdAsync(Guid id)
    {
        var service = await _context.Services
            .Include(s => s.Congregation)
                .ThenInclude(c => c.Denomination)
            .Include(s => s.Preacher)
            .Include(s => s.ChurchCalendarSunday)
            .Include(s => s.BibleTranslation)
            .Include(s => s.MusicalAccompaniment)
            .Include(s => s.SpecialOccasion)
            .Include(s => s.Bundles)
                .ThenInclude(b => b.Bundle)
            .Include(s => s.Elements)
                .ThenInclude(e => e.Label)
            .Include(s => s.Elements)
                .ThenInclude(e => e.Songs)
                    .ThenInclude(sg => sg.Bundle)
            .Include(s => s.Elements)
                .ThenInclude(e => e.Songs)
                    .ThenInclude(sg => sg.Verses)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return null;
        }

        return new ServiceDto(
            service.Id,
            service.Date,
            service.TimeOfDay.ToString(),
            new CongregationSummaryDto(
                service.Congregation.Id,
                service.Congregation.Name,
                service.Congregation.City,
                service.Congregation.Denomination?.Abbreviation),
            service.Preacher != null
                ? new PreacherSummaryDto(service.Preacher.Id, service.Preacher.FullName, service.Preacher.Title)
                : null,
            service.ChurchCalendarSunday?.Value,
            service.BibleTranslation?.Value,
            service.IsReadingService,
            service.ReadSermonBy,
            service.MusicalAccompaniment?.Value,
            service.HasBeamerLiturgy,
            service.HasBeamerTexts,
            service.HasBeamerSongs,
            service.HasBeamerTextsAndSongs,
            service.BroadcastUrl,
            service.SpecialOccasion?.Value,
            service.SermonText,
            service.SermonTheme,
            service.Notes,
            service.Bundles
                .OrderBy(b => b.Bundle.Value)
                .Select(b => b.Bundle.Value)
                .ToList(),
            service.Elements
                .OrderBy(e => e.Position)
                .Select(e => new ServiceElementDto(
                    e.Id,
                    e.Position,
                    e.ElementType.ToString(),
                    e.Label?.Value,
                    e.ScriptureReference,
                    e.Notes,
                    e.Songs
                        .OrderBy(sg => sg.Position)
                        .Select(sg => new ServiceElementSongDto(
                            sg.Id,
                            sg.Bundle.Value,
                            sg.Bundle.Abbreviation,
                            sg.SongNumber,
                            sg.Position,
                            sg.Verses
                                .OrderBy(v => v.Position)
                                .Select(v => v.VerseLabel)
                                .ToList()))
                        .ToList()))
                .ToList(),
            service.CreatedAt,
            service.CreatedBy);
    }

    public async Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, string userId)
    {
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Date = request.Date,
            TimeOfDay = (TimeOfDay)request.TimeOfDay,
            CongregationId = request.CongregationId,
            PreacherId = request.PreacherId,
            ChurchCalendarSundayId = request.ChurchCalendarSundayId,
            BibleTranslationId = request.BibleTranslationId,
            IsReadingService = request.IsReadingService,
            ReadSermonBy = request.ReadSermonBy,
            MusicalAccompanimentId = request.MusicalAccompanimentId,
            HasBeamerLiturgy = request.HasBeamerLiturgy,
            HasBeamerTexts = request.HasBeamerTexts,
            HasBeamerSongs = request.HasBeamerSongs,
            HasBeamerTextsAndSongs = request.HasBeamerTextsAndSongs,
            BroadcastUrl = request.BroadcastUrl,
            SpecialOccasionId = request.SpecialOccasionId,
            SermonText = request.SermonText,
            SermonTheme = request.SermonTheme,
            Notes = request.Notes,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        if (request.BundleIds != null)
        {
            foreach (var bundleId in request.BundleIds)
            {
                service.Bundles.Add(new ServiceBundle
                {
                    ServiceId = service.Id,
                    BundleId = bundleId
                });
            }
        }

        if (request.Elements != null)
        {
            foreach (var elementRequest in request.Elements)
            {
                var element = new ServiceElement
                {
                    Id = Guid.NewGuid(),
                    ServiceId = service.Id,
                    Position = elementRequest.Position,
                    ElementType = (ElementType)elementRequest.ElementType,
                    LabelId = elementRequest.LabelId,
                    ScriptureReference = elementRequest.ScriptureReference,
                    Notes = elementRequest.Notes,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                if (elementRequest.Songs != null)
                {
                    foreach (var songRequest in elementRequest.Songs)
                    {
                        var song = new ServiceElementSong
                        {
                            Id = Guid.NewGuid(),
                            ServiceElementId = element.Id,
                            BundleId = songRequest.BundleId,
                            SongNumber = songRequest.SongNumber,
                            Position = songRequest.Position
                        };

                        if (songRequest.Verses != null)
                        {
                            for (var index = 0; index < songRequest.Verses.Count; index++)
                            {
                                song.Verses.Add(new SongVerse
                                {
                                    Id = Guid.NewGuid(),
                                    ServiceElementSongId = song.Id,
                                    VerseLabel = songRequest.Verses[index],
                                    Position = index + 1
                                });
                            }
                        }

                        element.Songs.Add(song);
                    }
                }

                service.Elements.Add(element);
            }
        }

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return await GetServiceByIdAsync(service.Id) ?? throw new InvalidOperationException();
    }

    public async Task<ServiceDto?> UpdateServiceAsync(Guid id, UpdateServiceRequest request, string userId)
    {
        var service = await _context.Services
            .Include(s => s.Bundles)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return null;
        }

        service.Date = request.Date;
        service.TimeOfDay = (TimeOfDay)request.TimeOfDay;
        service.CongregationId = request.CongregationId;
        service.PreacherId = request.PreacherId;
        service.ChurchCalendarSundayId = request.ChurchCalendarSundayId;
        service.BibleTranslationId = request.BibleTranslationId;
        service.IsReadingService = request.IsReadingService;
        service.ReadSermonBy = request.ReadSermonBy;
        service.MusicalAccompanimentId = request.MusicalAccompanimentId;
        service.HasBeamerLiturgy = request.HasBeamerLiturgy;
        service.HasBeamerTexts = request.HasBeamerTexts;
        service.HasBeamerSongs = request.HasBeamerSongs;
        service.HasBeamerTextsAndSongs = request.HasBeamerTextsAndSongs;
        service.BroadcastUrl = request.BroadcastUrl;
        service.SpecialOccasionId = request.SpecialOccasionId;
        service.SermonText = request.SermonText;
        service.SermonTheme = request.SermonTheme;
        service.Notes = request.Notes;
        service.ModifiedBy = userId;
        service.ModifiedAt = DateTime.UtcNow;

        if (request.BundleIds != null)
        {
            _context.ServiceBundles.RemoveRange(service.Bundles);
            service.Bundles.Clear();

            foreach (var bundleId in request.BundleIds)
            {
                service.Bundles.Add(new ServiceBundle
                {
                    ServiceId = service.Id,
                    BundleId = bundleId
                });
            }
        }

        await _context.SaveChangesAsync();
        return await GetServiceByIdAsync(id);
    }

    public async Task<bool> DeleteServiceAsync(Guid id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return false;
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return true;
    }
}
