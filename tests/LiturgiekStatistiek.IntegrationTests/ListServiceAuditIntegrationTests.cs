using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// List item CRUD must stamp audit fields (CreatedBy/ModifiedBy) and write a
/// ChangeHistory row for each mutation so list changes are traceable.
/// </summary>
[TestFixture]
public class ListServiceAuditIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private ApplicationDbContext _db = null!;
    private ListService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using (var seedContext = new ApplicationDbContext(_options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(seedContext);
        }

        _db = new ApplicationDbContext(_options);
        _sut = new ListService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task AddUpdateDelete_StampAuditFields_AndWriteChangeHistory()
    {
        var listId = (await _db.ListDefinitions.AsNoTracking()
            .FirstAsync(d => d.Name == "LiturgicalLabels")).Id;

        var created = await _sut.AddListItemAsync(
            new CreateListItemRequest(listId, "ZZ-TEST Onderdeel", null, 99), "alice");
        Assert.That(created.CreatedBy, Is.EqualTo("alice"));

        var updated = await _sut.UpdateListItemAsync(created.Id,
            new UpdateListItemRequest("ZZ-TEST Onderdeel gewijzigd", null, 99, true), "bob");
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.ModifiedBy, Is.EqualTo("bob"));

        var deleted = await _sut.DeleteListItemAsync(created.Id, "carol");
        Assert.That(deleted, Is.True);

        var history = await _db.ChangeHistory.AsNoTracking()
            .Where(h => h.EntityType == nameof(ListItem) && h.EntityId == created.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        Assert.That(history.Select(h => h.ChangeType), Is.EqualTo(new[]
        {
            ChangeType.Created, ChangeType.Updated, ChangeType.Deleted
        }));
        Assert.That(history.Select(h => h.ChangedBy), Is.EqualTo(new[] { "alice", "bob", "carol" }));
        // The update and delete rows must snapshot the previous values.
        Assert.That(history[1].PreviousValues, Does.Contain("ZZ-TEST Onderdeel"));
        Assert.That(history[2].PreviousValues, Does.Contain("gewijzigd"));
    }
}
