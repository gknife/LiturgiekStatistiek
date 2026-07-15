using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Application.Services;
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
        DateOnly? toDate = null,
        Guid? denominationId = null,
        bool includeConcepts = true)
    {
        var query = _context.Services
            .Include(s => s.Congregation)
                .ThenInclude(c => c.Denomination)
            .Include(s => s.Preacher)
            .Include(s => s.SpecialOccasion)
            .AsQueryable();

        if (congregationId.HasValue)
        {
            query = query.Where(s => s.CongregationId == congregationId.Value);
        }

        if (denominationId.HasValue)
        {
            query = query.Where(s => s.Congregation.DenominationId == denominationId.Value);
        }

        if (!includeConcepts)
        {
            query = query.Where(s => s.Status == ServiceStatus.Gepubliceerd);
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
                s.BroadcastUrl,
                s.Congregation.Denomination != null ? s.Congregation.Denomination.Value : null,
                s.Status.ToString(),
                (int)s.Status,
                s.CreatedBy,
                s.CreatedAt,
                s.ModifiedBy,
                s.ModifiedAt))
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
            .Include(s => s.MusicalAccompaniment)
            .Include(s => s.SpecialOccasion)
            .Include(s => s.Bundles)
                .ThenInclude(b => b.Bundle)
            .Include(s => s.Elements)
                .ThenInclude(e => e.Label)
            .Include(s => s.Elements)
                .ThenInclude(e => e.Performer)
            .Include(s => s.Elements)
                .ThenInclude(e => e.BibleTranslation)
            .Include(s => s.Elements)
                .ThenInclude(e => e.ReadingReferences)
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

        var completeness = await BuildCompletenessLookupAsync(service);

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
                ? new PreacherSummaryDto(service.Preacher.Id, service.Preacher.FullName, service.Preacher.City)
                : null,
            service.ChurchCalendarSunday?.Value,
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
                                .ToList(),
                            sg.BundleId,
                            completeness.TryGetValue(SongKey(sg), out var comp) ? comp : null,
                            sg.SungInFull))
                        .ToList(),
                    e.LabelId,
                    e.PerformerId,
                    e.Performer?.Value,
                    e.IsBeurtzang,
                    e.BibleTranslationId,
                    e.BibleTranslation?.Value,
                    e.ReadingReferences
                        .OrderBy(r => r.Position)
                        .Select(r => new ReadingReferenceDto(
                            r.BibleBookId,
                            r.BookName,
                            r.Chapter,
                            r.VerseStart,
                            r.VerseEnd,
                            r.Position))
                        .ToList()))
                .ToList(),
            service.CreatedAt,
            service.CreatedBy,
            (int)service.TimeOfDay,
            service.ChurchCalendarSundayId,
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
                .ToList(),
            service.Status.ToString(),
            (int)service.Status,
            service.Congregation.DenominationId,
            service.Congregation.Denomination?.Value);
    }

    private static (Guid Bundle, string Section, int Number) SongKey(ServiceElementSong sg)
        => (sg.BundleId, sg.Section ?? "", sg.SongNumber);

    /// <summary>
    /// Computes per-song completeness for every sung song in a service. The catalog
    /// verse-count comes from the <see cref="Song"/> catalog; completeness is flagged
    /// both within a single onderdeel and across the whole service.
    /// </summary>
    private async Task<Dictionary<(Guid, string, int), SongCompletenessDto>> BuildCompletenessLookupAsync(Service service)
    {
        var songs = service.Elements.SelectMany(e => e.Songs).ToList();
        var result = new Dictionary<(Guid, string, int), SongCompletenessDto>();
        if (songs.Count == 0) return result;

        var keys = songs.Select(SongKey).Distinct().ToList();
        var bundleIds = keys.Select(k => k.Item1).Distinct().ToList();

        var catalog = await _context.Songs
            .Where(s => bundleIds.Contains(s.BundleId))
            .Select(s => new { s.BundleId, s.Section, s.Number, s.NumberOfVerses })
            .ToListAsync();

        foreach (var key in keys)
        {
            var catalogVerseCount = catalog
                .Where(c => c.BundleId == key.Item1 && (c.Section ?? "") == key.Item2 && c.Number == key.Item3)
                .Select(c => c.NumberOfVerses)
                .FirstOrDefault();

            var serviceVerses = songs
                .Where(sg => SongKey(sg) == key)
                .SelectMany(sg => sg.Verses.Select(v => v.VerseLabel))
                .ToList();

            // Per-onderdeel completeness is computed against the union within that
            // onderdeel; take the first element occurrence for the element scope.
            var elementVerses = songs
                .Where(sg => SongKey(sg) == key)
                .GroupBy(sg => sg.ServiceElementId)
                .Select(g => g.SelectMany(sg => sg.Verses.Select(v => v.VerseLabel)).ToList())
                .OrderByDescending(l => l.Count)
                .First();

            var sungInFull = songs.Where(sg => SongKey(sg) == key).Any(sg => sg.SungInFull);

            var comp = SongCompletenessCalculator.Compute(catalogVerseCount, elementVerses, serviceVerses, sungInFull);
            result[key] = new SongCompletenessDto(
                comp.State,
                comp.CompleteInElement,
                comp.CompleteInService,
                comp.CatalogVerseCount,
                comp.SungVerseCount);
        }

        return result;
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
            Status = request.Status.HasValue ? (ServiceStatus)request.Status.Value : ServiceStatus.Gepubliceerd,
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
                service.Elements.Add(BuildServiceElement(elementRequest, service.Id, userId));
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

    /// <summary>Builds a fully-populated (but untracked) service element graph from a request.</summary>
    private static ServiceElement BuildServiceElement(CreateServiceElementRequest elementRequest, Guid serviceId, string userId)
    {
        var element = new ServiceElement
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Position = elementRequest.Position,
            ElementType = (ElementType)elementRequest.ElementType,
            LabelId = elementRequest.LabelId,
            ScriptureReference = elementRequest.ScriptureReference,
            Notes = elementRequest.Notes,
            PerformerId = elementRequest.PerformerId,
            IsBeurtzang = elementRequest.IsBeurtzang,
            BibleTranslationId = elementRequest.BibleTranslationId,
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
                    Position = songRequest.Position,
                    SungInFull = songRequest.SungInFull
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

        if (elementRequest.ReadingReferences != null)
        {
            foreach (var r in elementRequest.ReadingReferences)
            {
                element.ReadingReferences.Add(new ReadingReference
                {
                    Id = Guid.NewGuid(),
                    ServiceElementId = element.Id,
                    Position = r.Position,
                    BibleBookId = r.BibleBookId,
                    BookName = r.BookName,
                    Chapter = r.Chapter,
                    VerseStart = r.VerseStart,
                    VerseEnd = r.VerseEnd
                });
            }
        }

        return element;
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

        var previousCongregationId = service.CongregationId;
        var previousPreacherId = service.PreacherId;

        service.Date = request.Date;
        service.TimeOfDay = (TimeOfDay)request.TimeOfDay;
        service.CongregationId = request.CongregationId;
        service.PreacherId = request.PreacherId;
        service.ChurchCalendarSundayId = request.ChurchCalendarSundayId;
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
        if (request.Status.HasValue)
        {
            service.Status = (ServiceStatus)request.Status.Value;
        }
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
                // Add via the DbSet (not the tracked service.Elements navigation) so the
                // entire new graph - element, songs and verses - is marked Added.
                _context.ServiceElements.Add(BuildServiceElement(elementRequest, service.Id, userId));
            }
        }

        if (request.SermonTextReferences != null)
        {
            foreach (var refRequest in request.SermonTextReferences)
            {
                // Add via the DbSet (not the tracked service.SermonTextReferences
                // navigation): after the delete was flushed above, adding a fresh row
                // through the tracked parent makes EF's identity resolution treat it as
                // an existing row to UPDATE, which throws DbUpdateConcurrencyException
                // ("expected to affect 1 row(s), but actually affected 0 row(s)").
                // This mirrors how the Elements graph is re-added.
                _context.SermonTextReferences.Add(new SermonTextReference
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

        // Remove gemeente/voorganger that no longer have any service after a reassign.
        await CleanupOrphansAsync(previousCongregationId, previousPreacherId);

        return await GetServiceByIdAsync(id);
    }

    /// <summary>
    /// Hard-deletes a congregation and/or preacher when they have no services left.
    /// Scope is limited to these auto-created reference entities; curated Lijsten are
    /// never touched.
    /// </summary>
    private async Task CleanupOrphansAsync(Guid? congregationId, Guid? preacherId)
    {
        var changed = false;

        if (congregationId.HasValue &&
            !await _context.Services.AnyAsync(s => s.CongregationId == congregationId.Value))
        {
            var congregation = await _context.Congregations.FindAsync(congregationId.Value);
            if (congregation != null)
            {
                _context.Congregations.Remove(congregation);
                changed = true;
            }
        }

        if (preacherId.HasValue &&
            !await _context.Services.AnyAsync(s => s.PreacherId == preacherId.Value))
        {
            var preacher = await _context.Preachers.FindAsync(preacherId.Value);
            if (preacher != null)
            {
                _context.Preachers.Remove(preacher);
                changed = true;
            }
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
        }
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

        var affectedCongregations = services.Select(s => s.CongregationId).Distinct().ToList();
        var affectedPreachers = services.Where(s => s.PreacherId.HasValue).Select(s => s.PreacherId!.Value).Distinct().ToList();

        _context.Services.RemoveRange(services);
        await _context.SaveChangesAsync();

        foreach (var cid in affectedCongregations)
        {
            await CleanupOrphansAsync(cid, null);
        }
        foreach (var pid in affectedPreachers)
        {
            await CleanupOrphansAsync(null, pid);
        }

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

    public async Task<ServiceDto?> PublishServiceAsync(Guid id, string userId)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return null;
        }

        service.Status = ServiceStatus.Gepubliceerd;
        service.ModifiedBy = userId;
        service.ModifiedAt = DateTime.UtcNow;
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

        var congregationId = service.CongregationId;
        var preacherId = service.PreacherId;

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        await CleanupOrphansAsync(congregationId, preacherId);
        return true;
    }
}
