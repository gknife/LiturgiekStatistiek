using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Verifies that <see cref="DataSeeder.SeedAsync"/> is idempotent: it backfills
/// system lists/items that were missing (e.g. a production DB seeded at an
/// earlier version) without creating duplicates when run repeatedly.
/// </summary>
[TestFixture]
public class DataSeederIdempotencyIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    private static readonly string[] SystemLists =
    {
        "SongBundles", "Denominations", "SpecialOccasions", "ServicePerformer",
        "ServiceOccasion", "BibleTranslations", "MusicalAccompaniment",
        "ChurchCalendarSundays", "LiturgicalLabels",
    };

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    [TearDown]
    public void TearDown() => _connection.Dispose();

    [Test]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateSystemListsOrItems()
    {
        await using (var ctx = new ApplicationDbContext(_options))
        {
            await ctx.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(ctx);
        }

        int defCountAfterFirst, itemCountAfterFirst;
        await using (var ctx = new ApplicationDbContext(_options))
        {
            defCountAfterFirst = await ctx.ListDefinitions.CountAsync();
            itemCountAfterFirst = await ctx.ListItems.CountAsync();
        }

        await using (var ctx = new ApplicationDbContext(_options))
        {
            await DataSeeder.SeedAsync(ctx);
        }

        await using (var verify = new ApplicationDbContext(_options))
        {
            Assert.That(await verify.ListDefinitions.CountAsync(), Is.EqualTo(defCountAfterFirst),
                "Re-running the seeder must not add duplicate list definitions.");
            Assert.That(await verify.ListItems.CountAsync(), Is.EqualTo(itemCountAfterFirst),
                "Re-running the seeder must not add duplicate list items.");
        }
    }

    [Test]
    public async Task SeedAsync_WithMissingSystemList_BackfillsItWithoutTouchingExisting()
    {
        // Simulate a database seeded at an earlier version by removing a list
        // that was added later, plus one item from a list that still exists.
        await using (var ctx = new ApplicationDbContext(_options))
        {
            await ctx.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(ctx);
        }

        await using (var ctx = new ApplicationDbContext(_options))
        {
            var calendar = await ctx.ListDefinitions
                .Include(d => d.Items)
                .FirstAsync(d => d.Name == "ChurchCalendarSundays");
            ctx.ListItems.RemoveRange(calendar.Items);
            ctx.ListDefinitions.Remove(calendar);

            var accompItem = await ctx.ListItems
                .FirstAsync(i => i.Value == "Band");
            ctx.ListItems.Remove(accompItem);

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new ApplicationDbContext(_options))
        {
            await DataSeeder.SeedAsync(ctx);
        }

        await using (var verify = new ApplicationDbContext(_options))
        {
            foreach (var name in SystemLists)
            {
                Assert.That(await verify.ListDefinitions.AnyAsync(d => d.Name == name), Is.True,
                    $"System list '{name}' should exist after backfill.");
            }

            var restored = await verify.ListDefinitions
                .Include(d => d.Items)
                .FirstAsync(d => d.Name == "ChurchCalendarSundays");
            Assert.That(restored.Items.Count, Is.EqualTo(23),
                "The removed ChurchCalendarSundays list should be fully restored.");

            Assert.That(await verify.ListItems.AnyAsync(i => i.Value == "Band"), Is.True,
                "A removed item within an existing system list should be backfilled.");
        }
    }
}
