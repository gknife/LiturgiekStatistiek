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
                s.Elements.Count,
                s.BroadcastUrl))
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
            .Include(s => s.SermonTextReferences)
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
                            sg.Section,
                            sg.SongNumber,
                            sg.Position,
                            sg.Verses
                                .OrderBy(v => v.Position)
                                .Select(v => v.VerseLabel)
                                .ToList()))
                        .ToList()))
                .ToList(),
            service.CreatedAt,
            service.CreatedBy,
            (int)service.TimeOfDay,
            service.ChurchCalendarSundayId,
            service.BibleTranslationId,
            service.MusicalAccompanimentId,
            service.SpecialOccasionId,
            service.SermonTextReferences
                .OrderBy(r => r.Position)
                .Select(r => new SermonTextReferenceDto(
                    r.BibleBookId,
                    r.BookName,
                    r.Chapter,
                    r.VerseStart,
                    r.VerseEnd,
                    r.Position))
                .ToList());
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
                            Section = songRequest.Section ?? "",
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

        if (request.SermonTextReferences != null)
        {
            foreach (var refRequest in request.SermonTextReferences)
            {
                service.SermonTextReferences.Add(new SermonTextReference
                {
                    Id = Guid.NewGuid(),
                    ServiceId = service.Id,
                    Position = refRequest.Position,
                    BibleBookId = refRequest.BibleBookId,
                    BookName = refRequest.BookName,
                    Chapter = refRequest.Chapter,
                    VerseStart = refRequest.VerseStart,
                    VerseEnd = refRequest.VerseEnd
                });
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
            .Include(s => s.Elements)
            .Include(s => s.SermonTextReferences)
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

        // Replace-collections edit: delete the existing child rows and flush them in
        // their own SaveChanges BEFORE inserting the replacements. Deleting the old
        // graph and inserting the new one in a single SaveChanges makes EF's identity
        // resolution treat a freshly-created child as an existing row to UPDATE (and
        // deleting every tracked level races the ON DELETE CASCADE on relational
        // providers), both of which throw DbUpdateConcurrencyException
        // ("expected to affect 1 row(s), but actually affected 0 row(s)"). Only the
        // top-level rows are removed explicitly; the database cascade removes the
        // child Songs and Verses.
        if (request.BundleIds != null)
        {
            _context.ServiceBundles.RemoveRange(service.Bundles);
            service.Bundles.Clear();
        }

        if (request.Elements != null)
        {
            _context.ServiceElements.RemoveRange(service.Elements);
            service.Elements.Clear();
        }

        if (request.SermonTextReferences != null)
        {
            _context.SermonTextReferences.RemoveRange(service.SermonTextReferences);
            service.SermonTextReferences.Clear();
        }

        await _context.SaveChangesAsync();

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
                            Section = songRequest.Section ?? "",
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

                // Add via the DbSet (not the tracked service.Elements navigation) so the
                // entire new graph - element, songs and verses - is marked Added. Adding
                // through the navigation only forces the element to Added; the grandchild
                // songs/verses keep their pre-set Guid keys and hit EF's "key is set =>
                // existing row" heuristic, producing a 0-row UPDATE that throws.
                _context.ServiceElements.Add(element);
            }
        }

        if (request.SermonTextReferences != null)
        {
            foreach (var refRequest in request.SermonTextReferences)
            {
                service.SermonTextReferences.Add(new SermonTextReference
                {
                    Id = Guid.NewGuid(),
                    ServiceId = service.Id,
                    Position = refRequest.Position,
                    BibleBookId = refRequest.BibleBookId,
                    BookName = refRequest.BookName,
                    Chapter = refRequest.Chapter,
                    VerseStart = refRequest.VerseStart,
                    VerseEnd = refRequest.VerseEnd
                });
            }
        }

        await _context.SaveChangesAsync();
        return await GetServiceByIdAsync(id);
    }

    public async Task<BulkOperationResult> BulkUpdateAsync(BulkUpdateServicesRequest request, string userId)
    {
        if (request.ServiceIds == null || request.ServiceIds.Count == 0)
            return new BulkOperationResult(0);

        var services = await _context.Services
            .Where(s => request.ServiceIds.Contains(s.Id))
            .ToListAsync();

        foreach (var service in services)
        {
            ApplyBulkField(service, request.Field, request.Value);
            service.ModifiedBy = userId;
            service.ModifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return new BulkOperationResult(services.Count);
    }

    public async Task<BulkOperationResult> BulkDeleteAsync(BulkDeleteServicesRequest request)
    {
        if (request.ServiceIds == null || request.ServiceIds.Count == 0)
            return new BulkOperationResult(0);

        var services = await _context.Services
            .Where(s => request.ServiceIds.Contains(s.Id))
            .ToListAsync();

        _context.Services.RemoveRange(services);
        await _context.SaveChangesAsync();
        return new BulkOperationResult(services.Count);
    }

    private static void ApplyBulkField(Service service, string field, string? value)
    {
        switch (field?.ToLowerInvariant())
        {
            case "timeofday":
                if (int.TryParse(value, out var tod) && Enum.IsDefined(typeof(TimeOfDay), tod))
                    service.TimeOfDay = (TimeOfDay)tod;
                break;
            case "congregationid":
                if (Guid.TryParse(value, out var cid)) service.CongregationId = cid;
                break;
            case "preacherid":
                service.PreacherId = Guid.TryParse(value, out var pid) ? pid : null;
                break;
            case "bibletranslationid":
                service.BibleTranslationId = Guid.TryParse(value, out var bid) ? bid : null;
                break;
            case "specialoccasionid":
                service.SpecialOccasionId = Guid.TryParse(value, out var sid) ? sid : null;
                break;
            case "musicalaccompanimentid":
                service.MusicalAccompanimentId = Guid.TryParse(value, out var mid) ? mid : null;
                break;
            case "isreadingservice":
                service.IsReadingService = bool.TryParse(value, out var b) && b;
                break;
            default:
                throw new ArgumentException($"Onbekend of niet-toegestaan veld voor bulk-bewerking: '{field}'.");
        }
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
