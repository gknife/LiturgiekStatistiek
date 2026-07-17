using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// A leesdienst (reading service) has no voorganger. The service layer must drop
/// any supplied preacher on both create and update, and the list summary must
/// expose the reading-service flag so the grid can flag it.
/// </summary>
[TestFixture]
public class ServiceServiceReadingServiceIntegrationTests
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _db = null!;
    private ServiceService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(seedContext);
        }

        _db = new ApplicationDbContext(options);
        _sut = new ServiceService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task CreateServiceAsync_ReadingService_DropsSuppliedPreacher()
    {
        var congregationId = (await _db.Congregations.AsNoTracking().FirstAsync()).Id;
        var preacherId = (await _db.Preachers.AsNoTracking().FirstAsync()).Id;

        var created = await _sut.CreateServiceAsync(
            BuildCreateRequest(congregationId, isReadingService: true, preacherId: preacherId), "tester");

        var reloaded = await _db.Services.AsNoTracking().FirstAsync(s => s.Id == created.Id);
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.IsReadingService, Is.True);
            Assert.That(reloaded.PreacherId, Is.Null,
                "A leesdienst must not keep a voorganger even when one is supplied.");
        });
    }

    [Test]
    public async Task UpdateServiceAsync_TurningIntoReadingService_ClearsExistingPreacher()
    {
        var congregationId = (await _db.Congregations.AsNoTracking().FirstAsync()).Id;
        var preacherId = (await _db.Preachers.AsNoTracking().FirstAsync()).Id;

        // Start as a normal service with a preacher.
        var created = await _sut.CreateServiceAsync(
            BuildCreateRequest(congregationId, isReadingService: false, preacherId: preacherId), "tester");
        var beforeUpdate = await _db.Services.AsNoTracking().FirstAsync(s => s.Id == created.Id);
        Assert.That(beforeUpdate.PreacherId, Is.EqualTo(preacherId));

        // Flip it to a leesdienst; the preacher must be cleared even though still supplied.
        var updated = await _sut.UpdateServiceAsync(created.Id,
            BuildUpdateRequest(congregationId, isReadingService: true, preacherId: preacherId), "tester");
        Assert.That(updated, Is.Not.Null);

        var reloaded = await _db.Services.AsNoTracking().FirstAsync(s => s.Id == created.Id);
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.IsReadingService, Is.True);
            Assert.That(reloaded.PreacherId, Is.Null);
        });
    }

    [Test]
    public async Task GetServicesAsync_Summary_ExposesReadingServiceFlag()
    {
        var congregationId = (await _db.Congregations.AsNoTracking().FirstAsync()).Id;

        var created = await _sut.CreateServiceAsync(
            BuildCreateRequest(congregationId, isReadingService: true, preacherId: null), "tester");

        var page = await _sut.GetServicesAsync(includeConcepts: true);
        var summary = page.Items.Single(s => s.Id == created.Id);

        Assert.Multiple(() =>
        {
            Assert.That(summary.IsReadingService, Is.True,
                "The grid relies on this flag to badge a leesdienst in the Voorganger column.");
            Assert.That(summary.PreacherName, Is.Null);
        });
    }

    private static CreateServiceRequest BuildCreateRequest(Guid congregationId, bool isReadingService, Guid? preacherId) =>
        new(
            Date: new DateOnly(2026, 3, 1),
            TimeOfDay: (int)TimeOfDay.Morning,
            CongregationId: congregationId,
            PreacherId: preacherId,
            ChurchCalendarSundayId: null,
            IsReadingService: isReadingService,
            ReadSermonBy: isReadingService ? "ouderling" : null,
            MusicalAccompanimentId: null,
            HasBeamerLiturgy: false,
            HasBeamerTexts: false,
            HasBeamerSongs: false,
            HasBeamerTextsAndSongs: false,
            BroadcastUrl: null,
            SpecialOccasionId: null,
            SermonText: null,
            SermonTheme: null,
            Notes: "ZZ-TEST leesdienst",
            BundleIds: null,
            Elements: null,
            SermonTextReferences: null,
            Status: 1);

    private static UpdateServiceRequest BuildUpdateRequest(Guid congregationId, bool isReadingService, Guid? preacherId) =>
        new(
            Date: new DateOnly(2026, 3, 1),
            TimeOfDay: (int)TimeOfDay.Morning,
            CongregationId: congregationId,
            PreacherId: preacherId,
            ChurchCalendarSundayId: null,
            IsReadingService: isReadingService,
            ReadSermonBy: isReadingService ? "ouderling" : null,
            MusicalAccompanimentId: null,
            HasBeamerLiturgy: false,
            HasBeamerTexts: false,
            HasBeamerSongs: false,
            HasBeamerTextsAndSongs: false,
            BroadcastUrl: null,
            SpecialOccasionId: null,
            SermonText: null,
            SermonTheme: null,
            Notes: "ZZ-TEST leesdienst update",
            BundleIds: null,
            Elements: new List<CreateServiceElementRequest>(),
            SermonTextReferences: null);
}
