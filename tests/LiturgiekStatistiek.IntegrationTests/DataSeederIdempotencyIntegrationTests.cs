using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Verifies <see cref="DataSeeder.SeedAsync"/> only seeds a completely empty
/// database and is a no-op on any database that already holds data, so it can
/// never overwrite, revert, or duplicate production data.
/// </summary>
[TestFixture]
public class DataSeederIdempotencyIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

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
    public async Task SeedAsync_OnNonEmptyDatabase_DoesNotReAddRemovedListsOrItems()
    {
        // Seed once (fresh DB), then simulate an admin deleting a system list and
        // an item. The database now still holds data, so a subsequent seed must be
        // a no-op and must not resurrect the removed rows.
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
            Assert.That(await verify.ListDefinitions.AnyAsync(d => d.Name == "ChurchCalendarSundays"), Is.False,
                "The seeder must not re-create a list an admin deleted from a non-empty database.");
            Assert.That(await verify.ListItems.AnyAsync(i => i.Value == "Band"), Is.False,
                "The seeder must not re-create an item an admin deleted from a non-empty database.");
        }
    }

    [Test]
    public async Task SeedAsync_ClassifiesLiturgicalLabelsByElementType()
    {
        await using (var ctx = new ApplicationDbContext(_options))
        {
            await ctx.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(ctx);
        }

        await using var verify = new ApplicationDbContext(_options);
        var labels = await verify.ListDefinitions
            .Include(d => d.Items)
            .FirstAsync(d => d.Name == "LiturgicalLabels");

        int? TypeOf(string value) => (int?)labels.Items.First(i => i.Value == value).LiturgicalElementType;

        Assert.That(TypeOf("Openingslied"), Is.EqualTo((int)ElementType.Song));
        Assert.That(TypeOf("Schriftlezing(en)"), Is.EqualTo((int)ElementType.Reading));
        Assert.That(TypeOf("Dankgebed"), Is.EqualTo((int)ElementType.Prayer));
        Assert.That(TypeOf("Votum"), Is.EqualTo((int)ElementType.LiturgicalAct));
        Assert.That(TypeOf("Thema preek"), Is.EqualTo((int)ElementType.Other));
    }

    [Test]
    public async Task SeedAsync_SeedsBibleBooks_Idempotently()
    {
        await using (var ctx = new ApplicationDbContext(_options))
        {
            await ctx.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(ctx);
        }

        int countAfterFirst;
        await using (var ctx = new ApplicationDbContext(_options))
        {
            countAfterFirst = await ctx.BibleBooks.CountAsync();
            Assert.That(countAfterFirst, Is.GreaterThan(0),
                "Bible books must be seeded independently of demo data so the reading dropdowns are populated.");
        }

        await using (var ctx = new ApplicationDbContext(_options))
        {
            await DataSeeder.SeedAsync(ctx);
        }

        await using (var verify = new ApplicationDbContext(_options))
        {
            Assert.That(await verify.BibleBooks.CountAsync(), Is.EqualTo(countAfterFirst),
                "Re-running the seeder must not duplicate Bible books.");
        }
    }

    [Test]
    public async Task SeedAsync_OnNonEmptyDatabase_DoesNotReAddRemovedBibleBooks()
    {
        await using (var ctx = new ApplicationDbContext(_options))
        {
            await ctx.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(ctx);
        }

        await using (var ctx = new ApplicationDbContext(_options))
        {
            ctx.BibleBooks.RemoveRange(await ctx.BibleBooks.ToListAsync());
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new ApplicationDbContext(_options))
        {
            await DataSeeder.SeedAsync(ctx);
        }

        await using (var verify = new ApplicationDbContext(_options))
        {
            Assert.That(await verify.BibleBooks.CountAsync(), Is.EqualTo(0),
                "On a non-empty database the seeder must not resurrect deleted Bible books.");
        }
    }

    [Test]
    public async Task SeedAsync_OnNonEmptyDatabase_IsNoOp()
    {
        // Pre-populate a single congregation (no lists, no books) so the database
        // is not empty, then confirm the seeder leaves it completely untouched.
        await using (var ctx = new ApplicationDbContext(_options))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Congregations.Add(new Congregation { Id = Guid.NewGuid(), Name = "Bestaand", City = "Stad" });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new ApplicationDbContext(_options))
        {
            await DataSeeder.SeedAsync(ctx);
        }

        await using (var verify = new ApplicationDbContext(_options))
        {
            Assert.That(await verify.ListDefinitions.CountAsync(), Is.EqualTo(0),
                "The seeder must not add system lists to a database that already holds data.");
            Assert.That(await verify.BibleBooks.CountAsync(), Is.EqualTo(0),
                "The seeder must not add Bible books to a database that already holds data.");
            Assert.That(await verify.Congregations.CountAsync(), Is.EqualTo(1),
                "The pre-existing congregation must be preserved untouched.");
        }
    }
}
