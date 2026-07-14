using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Verifies that a preacher's woonplaats (City) round-trips through create and is
/// surfaced in the search summary that the add-service screen consumes.
/// </summary>
[TestFixture]
public class PreacherServiceCityIntegrationTests
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _db = null!;
    private PreacherService _sut = null!;

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
        _sut = new PreacherService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task CreatePreacherAsync_PersistsCity()
    {
        var created = await _sut.CreatePreacherAsync(
            new CreatePreacherRequest(FullName: "Ds. Testpreker", Title: null, DenominationId: null, City: "Barneveld"),
            "tester");

        Assert.That(created.City, Is.EqualTo("Barneveld"));

        var reloaded = await _sut.GetPreacherByIdAsync(created.Id);
        Assert.That(reloaded!.City, Is.EqualTo("Barneveld"));
    }

    [Test]
    public async Task SearchPreachersAsync_IncludesCity()
    {
        // Search only returns preachers that have at least one service, so pick a
        // seeded preacher, set its city, and confirm the summary carries it.
        var preacher = await _db.Preachers.FirstAsync(p => p.Services.Any());
        preacher.City = "Ede";
        await _db.SaveChangesAsync();

        var results = await _sut.SearchPreachersAsync(preacher.FullName);

        var match = results.Single(r => r.Id == preacher.Id);
        Assert.That(match.City, Is.EqualTo("Ede"));
    }
}
