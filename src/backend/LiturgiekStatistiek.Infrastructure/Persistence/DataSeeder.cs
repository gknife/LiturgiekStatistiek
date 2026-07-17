using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, bool includeDemoData = true)
    {
        // Never overwrite or mutate existing data: only seed a completely empty
        // database. On any database that already holds data (i.e. production) the
        // seeder is a no-op, so an admin's edits to system lists, congregations,
        // etc. can never be reverted or duplicated by a restart/redeploy.
        if (!await IsDatabaseEmptyAsync(db))
        {
            return;
        }

        await EnsureSystemListsAsync(db);
        await EnsureBibleBooksAsync(db);
        if (includeDemoData)
        {
            await SeedDemoDataAsync(db);
        }
    }

    /// <summary>
    /// True only when the database contains no seedable data at all. Guards the seeder
    /// so it never touches a database that already holds production data.
    /// </summary>
    private static async Task<bool> IsDatabaseEmptyAsync(ApplicationDbContext db)
    {
        return !await db.ListDefinitions.AnyAsync()
            && !await db.BibleBooks.AnyAsync()
            && !await db.Congregations.AnyAsync()
            && !await db.Preachers.AnyAsync()
            && !await db.Services.AnyAsync()
            && !await db.Songs.AnyAsync()
            && !await db.ContentPages.AnyAsync();
    }

    /// <summary>
    /// Idempotently ensure the 66 canonical Bible books exist. Runs on every startup so a
    /// database seeded before books were added (or one that only ever received system lists,
    /// not demo data) is backfilled. Without this the book/chapter/verse dropdowns in the
    /// data-entry form have no options and appear broken.
    /// </summary>
    private static async Task EnsureBibleBooksAsync(ApplicationDbContext db)
    {
        if (await db.BibleBooks.AnyAsync()) return;

        foreach (var book in BibleData.GetBooks())
        {
            db.BibleBooks.Add(book);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db)
    {
        // Demo/sample data is seeded only into a fresh database; the system lists
        // are created/backfilled idempotently by EnsureSystemListsAsync.
        if (await db.Congregations.AnyAsync()) return;

        // Look up the system-list items the demo data wires up by foreign key.
        var bundleItems = await db.ListItems.Where(i => i.ListDefinition.Name == "SongBundles").ToListAsync();
        var denomItems = await db.ListItems.Where(i => i.ListDefinition.Name == "Denominations").ToListAsync();
        var labelItems = await db.ListItems.Where(i => i.ListDefinition.Name == "LiturgicalLabels").ToListAsync();
        ListItem Bundle(string abbr) => bundleItems.First(i => i.Abbreviation == abbr);
        ListItem Denom(string abbr) => denomItems.First(i => i.Abbreviation == abbr);

        var ps1773 = Bundle("Ps1773");
        var lvdK = Bundle("LvdK");
        var wk = Bundle("WK");
        var wkPs = Bundle("WKPs");
        var opw = Bundle("Opw");
        var gk = Bundle("GK");

        var pkn = Denom("PKN");
        var ngk = Denom("NGK");
        var gg = Denom("GG");

        // --- Congregations ---
        var cong1 = new Congregation
        {
            Id = Guid.NewGuid(), Name = "De Lichtbron", City = "Zutphen",
            DenominationId = ngk.Id, Latitude = 52.1485m, Longitude = 6.1960m
        };
        var cong2 = new Congregation
        {
            Id = Guid.NewGuid(), Name = "Gereformeerde Gemeente", City = "Apeldoorn",
            DenominationId = gg.Id, Latitude = 52.2112m, Longitude = 5.9699m
        };
        var cong3 = new Congregation
        {
            Id = Guid.NewGuid(), Name = "Nieuwe Kerk", City = "Putten",
            DenominationId = pkn.Id, Latitude = 52.2610m, Longitude = 5.6065m
        };
        db.Congregations.AddRange(cong1, cong2, cong3);

        // --- Preachers ---
        var preacher1 = new Preacher { Id = Guid.NewGuid(), FullName = "ds. Janneke Dekker" };
        var preacher2 = new Preacher { Id = Guid.NewGuid(), FullName = "ds. R.A.M. Visser" };
        var preacher3 = new Preacher { Id = Guid.NewGuid(), FullName = "prop. J. van der Knijff" };
        db.Preachers.AddRange(preacher1, preacher2, preacher3);

        // --- Sample Services ---
        // Service 1: Zutphen
        var svc1 = new Service
        {
            Id = Guid.NewGuid(), CongregationId = cong1.Id, PreacherId = preacher1.Id,
            Date = new DateOnly(2026, 6, 7), TimeOfDay = TimeOfDay.Afternoon,
            BroadcastUrl = "https://delichtbron-zutphen.nl/live-dienst-volgen/",
            SermonTheme = "Levend zijn in Christus",
            SermonText = "Filippensen 1:12-26"
        };
        db.Services.Add(svc1);

        // Elements for service 1
        var label_voorzang = labelItems.FirstOrDefault(l => l.Value == "Voorzang");
        var label_opening = labelItems.FirstOrDefault(l => l.Value == "Openingslied");
        var label_napreek = labelItems.FirstOrDefault(l => l.Value == "Na de preek");
        var label_slotlied = labelItems.FirstOrDefault(l => l.Value == "Slotlied");
        var label_zegen = labelItems.FirstOrDefault(l => l.Value == "Zegen");

        var el1_1 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc1.Id, Position = 1, LabelId = label_voorzang?.Id, ElementType = ElementType.Song };
        var el1_2 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc1.Id, Position = 2, LabelId = label_opening?.Id, ElementType = ElementType.Song };
        var el1_3 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc1.Id, Position = 3, ElementType = ElementType.Song, Notes = "Na viering" };
        var el1_4 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc1.Id, Position = 4, LabelId = label_napreek?.Id, ElementType = ElementType.Song };
        var el1_5 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc1.Id, Position = 5, ElementType = ElementType.Song, Notes = "Bij geloofsbelijdenis" };
        db.ServiceElements.AddRange(el1_1, el1_2, el1_3, el1_4, el1_5);

        // Songs for service 1
        var song1_1 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el1_1.Id, BundleId = lvdK.Id, SongNumber = 360, Position = 1 };
        db.ServiceElementSongs.Add(song1_1);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_1.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_1.Id, VerseLabel = "2", Position = 2 }
        );

        var song1_2 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el1_2.Id, BundleId = opw.Id, SongNumber = 220, Position = 1 };
        db.ServiceElementSongs.Add(song1_2);

        var song1_3 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el1_3.Id, BundleId = lvdK.Id, SongNumber = 124, Position = 1 };
        db.ServiceElementSongs.Add(song1_3);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_3.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_3.Id, VerseLabel = "3", Position = 2 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_3.Id, VerseLabel = "4", Position = 3 }
        );

        var song1_4 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el1_4.Id, BundleId = lvdK.Id, SongNumber = 91, Position = 1 };
        db.ServiceElementSongs.Add(song1_4);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_4.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_4.Id, VerseLabel = "2", Position = 2 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_4.Id, VerseLabel = "4", Position = 3 }
        );

        var song1_5 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el1_5.Id, BundleId = gk.Id, SongNumber = 27, Position = 1 };
        db.ServiceElementSongs.Add(song1_5);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_5.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = song1_5.Id, VerseLabel = "7", Position = 2 }
        );

        // Service 2: Apeldoorn
        var svc2 = new Service
        {
            Id = Guid.NewGuid(), CongregationId = cong2.Id, PreacherId = preacher2.Id,
            Date = new DateOnly(2026, 6, 7), TimeOfDay = TimeOfDay.Morning,
            BroadcastUrl = "https://www.youtube.com/watch?v=v03pWdyW2KI",
            SermonTheme = "De genezing van een verlamde man",
            SermonText = "Handelingen 3, 6-8"
        };
        db.Services.Add(svc2);

        var el2_1 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc2.Id, Position = 1, LabelId = label_opening?.Id, ElementType = ElementType.Song };
        var el2_2 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc2.Id, Position = 2, ElementType = ElementType.Song, Notes = "Na vermaan" };
        var el2_3 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc2.Id, Position = 3, ElementType = ElementType.Song, Notes = "Voor de preek" };
        var el2_4 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc2.Id, Position = 4, ElementType = ElementType.Song, Notes = "Tussenzang" };
        var el2_5 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc2.Id, Position = 5, LabelId = label_slotlied?.Id, ElementType = ElementType.Song };
        db.ServiceElements.AddRange(el2_1, el2_2, el2_3, el2_4, el2_5);

        var s2_1 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_1.Id, BundleId = ps1773.Id, Section = "Psalm", SongNumber = 63, Position = 1 };
        db.ServiceElementSongs.Add(s2_1);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_1.Id, VerseLabel = "2", Position = 1 });

        var s2_2 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_2.Id, BundleId = ps1773.Id, Section = "Psalm", SongNumber = 143, Position = 1 };
        db.ServiceElementSongs.Add(s2_2);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_2.Id, VerseLabel = "2", Position = 1 });

        var s2_3 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_3.Id, BundleId = ps1773.Id, Section = "Psalm", SongNumber = 145, Position = 1 };
        db.ServiceElementSongs.Add(s2_3);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_3.Id, VerseLabel = "2", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_3.Id, VerseLabel = "4", Position = 2 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_3.Id, VerseLabel = "6", Position = 3 }
        );

        var s2_4 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_4.Id, BundleId = ps1773.Id, Section = "Psalm", SongNumber = 146, Position = 1 };
        db.ServiceElementSongs.Add(s2_4);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_4.Id, VerseLabel = "6", Position = 1 });

        var s2_5 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_5.Id, BundleId = ps1773.Id, Section = "Psalm", SongNumber = 18, Position = 1 };
        db.ServiceElementSongs.Add(s2_5);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_5.Id, VerseLabel = "9", Position = 1 });

        // Service 3: Putten
        var svc3 = new Service
        {
            Id = Guid.NewGuid(), CongregationId = cong3.Id, PreacherId = preacher3.Id,
            Date = new DateOnly(2026, 6, 7), TimeOfDay = TimeOfDay.Morning,
            SermonTheme = "Gelokt naar de levensboom",
            SermonText = "Openbaring 2:1-7"
        };
        db.Services.Add(svc3);

        var el3_1 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc3.Id, Position = 1, LabelId = label_voorzang?.Id, ElementType = ElementType.Song };
        var el3_2 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc3.Id, Position = 2, LabelId = label_opening?.Id, ElementType = ElementType.Song };
        var el3_3 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc3.Id, Position = 3, ElementType = ElementType.Song, Notes = "Na vermaan" };
        var el3_4 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc3.Id, Position = 4, ElementType = ElementType.Song, Notes = "Voor de preek" };
        var el3_5 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc3.Id, Position = 5, LabelId = label_napreek?.Id, ElementType = ElementType.Song };
        var el3_6 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = svc3.Id, Position = 6, LabelId = label_slotlied?.Id, ElementType = ElementType.Song };
        db.ServiceElements.AddRange(el3_1, el3_2, el3_3, el3_4, el3_5, el3_6);

        // Voorzang: Ps1773 Gezang 9:1,7 + WK 73:1
        var s3_1a = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_1.Id, BundleId = ps1773.Id, Section = "Gezang", SongNumber = 9, Position = 1 };
        db.ServiceElementSongs.Add(s3_1a);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_1a.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_1a.Id, VerseLabel = "7", Position = 2 }
        );
        var s3_1b = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_1.Id, BundleId = wk.Id, SongNumber = 73, Position = 2 };
        db.ServiceElementSongs.Add(s3_1b);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_1b.Id, VerseLabel = "1", Position = 1 });

        // Opening: WKPs 103:1,2
        var s3_2 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_2.Id, BundleId = wkPs.Id, SongNumber = 103, Position = 1 };
        db.ServiceElementSongs.Add(s3_2);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_2.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_2.Id, VerseLabel = "2", Position = 2 }
        );

        // Na vermaan: WKPs 103:3
        var s3_3 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_3.Id, BundleId = wkPs.Id, SongNumber = 103, Position = 1 };
        db.ServiceElementSongs.Add(s3_3);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_3.Id, VerseLabel = "3", Position = 1 });

        // Voor de preek: Ps1773 133:1,2,3
        var s3_4 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_4.Id, BundleId = ps1773.Id, Section = "Psalm", SongNumber = 133, Position = 1 };
        db.ServiceElementSongs.Add(s3_4);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_4.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_4.Id, VerseLabel = "2", Position = 2 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_4.Id, VerseLabel = "3", Position = 3 }
        );

        // Na de preek: WK 230:1-5
        var s3_5 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_5.Id, BundleId = wk.Id, SongNumber = 230, Position = 1 };
        db.ServiceElementSongs.Add(s3_5);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_5.Id, VerseLabel = "1", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_5.Id, VerseLabel = "2", Position = 2 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_5.Id, VerseLabel = "3", Position = 3 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_5.Id, VerseLabel = "4", Position = 4 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_5.Id, VerseLabel = "5", Position = 5 }
        );

        // Slotlied: WKPs 103:9
        var s3_6 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_6.Id, BundleId = wkPs.Id, SongNumber = 103, Position = 1 };
        db.ServiceElementSongs.Add(s3_6);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s3_6.Id, VerseLabel = "9", Position = 1 });

        // --- Demo services covering the example questions (Psalm 6, 116, 119, 150) ---
        // Helper to add a Ps1773 psalm element with the given verse labels.
        void AddPsalm(Guid serviceId, int position, int psalmNumber, params string[] verseLabels)
        {
            var el = new ServiceElement { Id = Guid.NewGuid(), ServiceId = serviceId, Position = position, ElementType = ElementType.Song };
            db.ServiceElements.Add(el);
            var ses = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el.Id, BundleId = ps1773.Id, Section = "Psalm", SongNumber = psalmNumber, Position = 1 };
            db.ServiceElementSongs.Add(ses);
            for (var i = 0; i < verseLabels.Length; i++)
                db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = ses.Id, VerseLabel = verseLabels[i], Position = i + 1 });
        }

        var thisYear = DateTime.Now.Year;

        // svc4 — NGK (cong1), this year: Psalm 119 (meest gezongen couplet), Psalm 150, Psalm 6.
        var svc4 = new Service
        {
            Id = Guid.NewGuid(), CongregationId = cong1.Id, PreacherId = preacher1.Id,
            Date = new DateOnly(thisYear, 3, 9), TimeOfDay = TimeOfDay.Morning,
            SermonTheme = "Uw woord is een lamp", SermonText = "Psalm 119:105"
        };
        db.Services.Add(svc4);
        AddPsalm(svc4.Id, 1, 119, "1", "2", "3", "4");
        AddPsalm(svc4.Id, 2, 150, "1", "2");
        AddPsalm(svc4.Id, 3, 6, "1", "2", "3");

        // svc5 — PKN (cong3), last year: Psalm 116, Psalm 150, Psalm 119.
        var svc5 = new Service
        {
            Id = Guid.NewGuid(), CongregationId = cong3.Id, PreacherId = preacher3.Id,
            Date = new DateOnly(thisYear - 1, 9, 15), TimeOfDay = TimeOfDay.Evening,
            SermonTheme = "Ik heb den HEER lief", SermonText = "Psalm 116"
        };
        db.Services.Add(svc5);
        AddPsalm(svc5.Id, 1, 116, "1", "2", "3", "4");
        AddPsalm(svc5.Id, 2, 150, "1");
        AddPsalm(svc5.Id, 3, 119, "5");

        // svc6 — GG (cong2), two years ago: Psalm 116, Psalm 150.
        var svc6 = new Service
        {
            Id = Guid.NewGuid(), CongregationId = cong2.Id, PreacherId = preacher2.Id,
            Date = new DateOnly(thisYear - 2, 11, 3), TimeOfDay = TimeOfDay.Morning,
            SermonTheme = "Lofzang", SermonText = "Psalm 150"
        };
        db.Services.Add(svc6);
        AddPsalm(svc6.Id, 1, 116, "1", "2");
        AddPsalm(svc6.Id, 2, 150, "3");

        // --- Content pages ---
        // Guarded independently of the Congregations check above: the homepage has a
        // unique index on Slug, so re-running the demo seed on a database whose
        // congregations were emptied (but which still holds the content page) would
        // otherwise throw a duplicate-key exception and crash the app on startup.
        if (!await db.ContentPages.AnyAsync(c => c.Slug == "homepage"))
        {
            db.ContentPages.Add(new ContentPage
            {
                Id = Guid.NewGuid(),
                Slug = "homepage",
            TitleNl = "Welkom bij Liturgiek Statistiek",
            ContentMarkdown = @"# Liturgiek Statistiek

Welkom bij het onderzoeksplatform **Liturgiek Statistiek**. Dit project brengt de liturgische praktijk van kerken in Nederland in kaart door middel van een uitgebreide database van kerkdiensten.

## Wat kunt u hier vinden?

- **Zoeken**: Doorzoek de database met voorgedefinieerde sjablonen of stel uw eigen vraag in het Nederlands.
- **Statistieken**: Ontdek patronen in liedkeuze, psalmgebruik, en liturgische praktijken.
- **Bijdragen**: Help het onderzoek door liturgieën van kerkdiensten in te voeren.

## Over het onderzoek

Dit platform is ontwikkeld ten behoeve van wetenschappelijk onderzoek naar de liturgische praktijk in Nederlandse kerken. Het verzamelt gegevens over kerkdiensten, waaronder de gebruikte liederen, Schriftlezingen, en de volgorde van de liturgie.

### Onderzoeksvragen
- Welke liederen worden het meest gezongen in bepaalde kerkgenootschappen?
- Hoe verschilt het psalmgebruik tussen gemeenten?
- Welke trends zijn er in liedkeuze over de jaren?
- Wat is de relatie tussen seizoen/kerkelijk jaar en liedkeuze?
",
            ModifiedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        await SeedSongCatalogAsync(db, new (ListItem, string, string)[]
        {
            (ps1773, "Psalm", "psalmen-1773.json"),
            (ps1773, "Gezang", "enige-gezangen.json"),
            (lvdK, "", "liedboek-1973.json"),
            (wk, "", "weerklank.json"),
            (opw, "", "opwekking.json"),
            (gk, "", "gereformeerd-kerkboek.json"),
        });

        await SeedBundleSectionsAsync(db);

        // Psalm 18 (1773) has a Voorzang sung before verse 1 — model it as a named
        // catalog verse ordered ahead of verse 1 (SortOrder -1), excluded from the
        // numbered-verse completeness count.
        var psalm18 = await db.Songs
            .Include(s => s.Verses)
            .FirstOrDefaultAsync(s => s.BundleId == ps1773.Id && s.Section == "Psalm" && s.Number == 18);
        if (psalm18 != null && psalm18.Verses.All(v => v.Label != "Voorzang"))
        {
            db.SongCatalogVerses.Add(new SongCatalogVerse
            {
                Id = Guid.NewGuid(),
                SongId = psalm18.Id,
                Number = 0,
                Label = "Voorzang",
                SortOrder = -1
            });
            await db.SaveChangesAsync();
        }

        await SeedServiceTemplatesAsync(db, cong2.Id);
    }

    /// <summary>
    /// Seed the predefined service templates (sjablonen) per kerkgenootschap, occasion
    /// and dagdeel. Also seeds a leesdienst demo service so the Diensten grid badge and
    /// the leesdienst flow can be exercised. Runs only on a fresh database.
    /// </summary>
    private static async Task SeedServiceTemplatesAsync(ApplicationDbContext db, Guid ggCongregationId)
    {
        if (await db.ServiceTemplates.AnyAsync()) return;

        var denomItems = await db.ListItems.Where(i => i.ListDefinition.Name == "Denominations").ToListAsync();
        var bundleItems = await db.ListItems.Where(i => i.ListDefinition.Name == "SongBundles").ToListAsync();
        var translationItems = await db.ListItems.Where(i => i.ListDefinition.Name == "BibleTranslations").ToListAsync();
        var occasionItems = await db.ListItems.Where(i => i.ListDefinition.Name == "ServiceOccasion").ToListAsync();
        var performerItems = await db.ListItems.Where(i => i.ListDefinition.Name == "ServicePerformer").ToListAsync();
        var accompanimentItems = await db.ListItems.Where(i => i.ListDefinition.Name == "MusicalAccompaniment").ToListAsync();
        var labelItems = await db.ListItems.Where(i => i.ListDefinition.Name == "LiturgicalLabels").ToListAsync();

        Guid? Denom(string abbr) => denomItems.FirstOrDefault(i => i.Abbreviation == abbr)?.Id;
        Guid? Bundle(string abbr) => bundleItems.FirstOrDefault(i => i.Abbreviation == abbr)?.Id;
        Guid? Translation(string abbr) => translationItems.FirstOrDefault(i => i.Abbreviation == abbr)?.Id;
        Guid? Occasion(string value) => occasionItems.FirstOrDefault(i => i.Value == value)?.Id;
        Guid? Performer(string value) => performerItems.FirstOrDefault(i => i.Value == value)?.Id;
        Guid? Accompaniment(string value) => accompanimentItems.FirstOrDefault(i => i.Value == value)?.Id;
        Guid? Label(string value) => labelItems.FirstOrDefault(i => i.Value == value)?.Id;

        // A common ordre-van-dienst scaffold reused by the reguliere sjablonen.
        // Tuple: (label value, element type, is a song element).
        List<ServiceTemplateElement> Scaffold(Guid? performerId, IEnumerable<(string Label, ElementType Type)> rows)
        {
            var pos = 1;
            var list = new List<ServiceTemplateElement>();
            foreach (var (label, type) in rows)
            {
                list.Add(new ServiceTemplateElement
                {
                    Id = Guid.NewGuid(),
                    Position = pos++,
                    ElementType = type,
                    LabelId = Label(label),
                    PerformerId = type == ElementType.Song ? null : performerId
                });
            }
            return list;
        }

        var reguliereRows = new (string, ElementType)[]
        {
            ("Votum", ElementType.LiturgicalAct),
            ("Groet", ElementType.LiturgicalAct),
            ("Openingslied", ElementType.Song),
            ("Gebed om de opening van het Woord", ElementType.Prayer),
            ("Schriftlezing(en)", ElementType.Reading),
            ("Na de preek", ElementType.Song),
            ("Slotlied", ElementType.Song),
            ("Zegen", ElementType.LiturgicalAct),
        };

        var avondmaalRows = new (string, ElementType)[]
        {
            ("Votum", ElementType.LiturgicalAct),
            ("Groet", ElementType.LiturgicalAct),
            ("Openingslied", ElementType.Song),
            ("Schriftlezing(en)", ElementType.Reading),
            ("Na de preek", ElementType.Song),
            ("Tussenzang", ElementType.Song),
            ("Slotlied", ElementType.Song),
            ("Zegen", ElementType.LiturgicalAct),
        };

        var doopRows = new (string, ElementType)[]
        {
            ("Votum", ElementType.LiturgicalAct),
            ("Groet", ElementType.LiturgicalAct),
            ("Openingslied", ElementType.Song),
            ("Schriftlezing(en)", ElementType.Reading),
            ("Na de preek", ElementType.Song),
            ("Slotlied", ElementType.Song),
            ("Zegen", ElementType.LiturgicalAct),
        };

        ServiceTemplate Template(
            string name, Guid? denomId, TimeOfDay? timeOfDay, Guid? occasionId,
            bool isReading, Guid? bundleId, Guid? translationId, Guid? performerId,
            Guid? accompanimentId, IEnumerable<(string, ElementType)> rows)
        {
            return new ServiceTemplate
            {
                Id = Guid.NewGuid(),
                Name = name,
                DenominationId = denomId,
                TimeOfDay = timeOfDay,
                OccasionId = occasionId,
                IsActive = true,
                IsReadingService = isReading,
                DefaultSongBundleId = bundleId,
                DefaultBibleTranslationId = translationId,
                MusicalAccompanimentId = accompanimentId,
                HasBeamerLiturgy = false,
                HasBeamerTexts = false,
                HasBeamerSongs = false,
                Elements = Scaffold(performerId, rows)
            };
        }

        var voorganger = Performer("Voorganger");
        var ouderling = Performer("Ouderling");

        var templates = new List<ServiceTemplate>
        {
            Template("PKN – Reguliere ochtenddienst", Denom("PKN"), TimeOfDay.Morning, null,
                false, Bundle("LvdK"), Translation("NBV21"), voorganger, Accompaniment("Orgel"), reguliereRows),
            Template("PKN – Reguliere avonddienst", Denom("PKN"), TimeOfDay.Evening, null,
                false, Bundle("LvdK"), Translation("NBV21"), voorganger, Accompaniment("Orgel"), reguliereRows),
            Template("PKN – Avondmaalsdienst", Denom("PKN"), null, Occasion("Avondmaal"),
                false, Bundle("LvdK"), Translation("NBV21"), voorganger, Accompaniment("Orgel"), avondmaalRows),
            Template("PKN – Doopdienst", Denom("PKN"), null, Occasion("Doop"),
                false, Bundle("LvdK"), Translation("NBV21"), voorganger, Accompaniment("Orgel"), doopRows),
            Template("GG – Reguliere leesdienst", Denom("GG"), TimeOfDay.Morning, null,
                true, Bundle("Ps1773"), Translation("SV"), ouderling, Accompaniment("Orgel"), reguliereRows),
            Template("GG – Avondmaalsdienst", Denom("GG"), null, Occasion("Avondmaal"),
                false, Bundle("Ps1773"), Translation("SV"), voorganger, Accompaniment("Orgel"), avondmaalRows),
            Template("NGK – Reguliere dienst", Denom("NGK"), TimeOfDay.Morning, null,
                false, Bundle("WK"), Translation("HSV"), voorganger, Accompaniment("Piano"), reguliereRows),
        };

        // A template that deliberately leaves the "Na de zegen (orgelspel of...)"
        // performer unset, so the frontend must preserve an explicit "geen voorganger"
        // instead of defaulting it to Voorganger when the dienst is created.
        templates.Add(new ServiceTemplate
        {
            Id = Guid.NewGuid(),
            Name = "PKN – Ochtenddienst met orgelspel na de zegen",
            DenominationId = Denom("PKN"),
            TimeOfDay = TimeOfDay.Morning,
            OccasionId = null,
            IsActive = true,
            IsReadingService = false,
            DefaultSongBundleId = Bundle("LvdK"),
            DefaultBibleTranslationId = Translation("NBV21"),
            MusicalAccompanimentId = Accompaniment("Orgel"),
            HasBeamerLiturgy = false,
            HasBeamerTexts = false,
            HasBeamerSongs = false,
            Elements = new List<ServiceTemplateElement>
            {
                new() { Id = Guid.NewGuid(), Position = 1, ElementType = ElementType.LiturgicalAct, LabelId = Label("Votum"), PerformerId = voorganger },
                new() { Id = Guid.NewGuid(), Position = 2, ElementType = ElementType.Song, LabelId = Label("Openingslied"), PerformerId = null },
                new() { Id = Guid.NewGuid(), Position = 3, ElementType = ElementType.Reading, LabelId = Label("Schriftlezing(en)"), PerformerId = voorganger },
                new() { Id = Guid.NewGuid(), Position = 4, ElementType = ElementType.Song, LabelId = Label("Slotlied"), PerformerId = null },
                new() { Id = Guid.NewGuid(), Position = 5, ElementType = ElementType.LiturgicalAct, LabelId = Label("Zegen"), PerformerId = voorganger },
                new() { Id = Guid.NewGuid(), Position = 6, ElementType = ElementType.LiturgicalAct, LabelId = Label("Na de zegen (orgelspel of...)"), PerformerId = null },
            }
        });

        db.ServiceTemplates.AddRange(templates);

        // A seeded leesdienst so the grid badge and leesdienst flow are testable and
        // stay available for future runs (no preacher).
        var leesLabelOpening = Label("Openingslied");
        var leesLabelSlot = Label("Slotlied");
        var ps1773Id = Bundle("Ps1773");
        var leesdienst = new Service
        {
            Id = Guid.NewGuid(),
            CongregationId = ggCongregationId,
            PreacherId = null,
            IsReadingService = true,
            Date = new DateOnly(DateTime.Now.Year, 2, 16),
            TimeOfDay = TimeOfDay.Afternoon,
            SermonTheme = "Leesdienst – preek gelezen",
            SermonText = "Zondag 1 Heidelbergse Catechismus"
        };
        db.Services.Add(leesdienst);
        var lesEl1 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = leesdienst.Id, Position = 1, LabelId = leesLabelOpening, ElementType = ElementType.Song };
        var lesEl2 = new ServiceElement { Id = Guid.NewGuid(), ServiceId = leesdienst.Id, Position = 2, LabelId = leesLabelSlot, ElementType = ElementType.Song };
        db.ServiceElements.AddRange(lesEl1, lesEl2);
        if (ps1773Id.HasValue)
        {
            var lesSong1 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = lesEl1.Id, BundleId = ps1773Id.Value, Section = "Psalm", SongNumber = 68, Position = 1 };
            var lesSong2 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = lesEl2.Id, BundleId = ps1773Id.Value, Section = "Psalm", SongNumber = 134, Position = 1 };
            db.ServiceElementSongs.AddRange(lesSong1, lesSong2);
            db.SongVerses.AddRange(
                new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = lesSong1.Id, VerseLabel = "1", Position = 1 },
                new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = lesSong2.Id, VerseLabel = "1", Position = 1 },
                new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = lesSong2.Id, VerseLabel = "2", Position = 2 },
                new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = lesSong2.Id, VerseLabel = "3", Position = 3 }
            );
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Create the per-bundle rubrieken (categorieën) from the distinct non-empty
    /// <see cref="Song.Section"/> values that were just seeded, marking the most-used
    /// rubriek per bundle as the default (pre-selected in the Lied dropdown).
    /// </summary>
    private static async Task SeedBundleSectionsAsync(ApplicationDbContext db)
    {
        if (await db.BundleSections.AnyAsync()) return;

        var groups = await db.Songs
            .Where(s => s.Section != "")
            .GroupBy(s => new { s.BundleId, s.Section })
            .Select(g => new { g.Key.BundleId, g.Key.Section, Count = g.Count() })
            .ToListAsync();

        foreach (var byBundle in groups.GroupBy(g => g.BundleId))
        {
            var ordered = byBundle.OrderByDescending(x => x.Count).ThenBy(x => x.Section).ToList();
            var order = 0;
            foreach (var item in ordered)
            {
                db.BundleSections.Add(new BundleSection
                {
                    Id = Guid.NewGuid(),
                    BundleId = item.BundleId,
                    Value = item.Section,
                    SortOrder = order,
                    IsDefault = order == 0,
                    IsActive = true
                });
                order++;
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Idempotently ensure every system list and its items exist. Runs on every
    /// startup so a database seeded at an earlier version is backfilled with lists
    /// (and list items) added later, without touching existing definitions/items.
    /// </summary>
    private static async Task EnsureSystemListsAsync(ApplicationDbContext db)
    {
        var canonical = new (string Name, string Description, (string Value, string? Abbrev)[] Items)[]
        {
            ("SongBundles", "Liedbundels", new[] {
                ("Psalmen 1773", (string?)"Ps1773"), ("Liedboek voor de Kerken", "LvdK"),
                ("Weerklank", "WK"), ("Weerklank Psalmen", "WKPs"),
                ("Opwekking", "Opw"), ("Gereformeerd Kerkboek", "GK") }),
            ("Denominations", "Kerkgenootschappen", new[] {
                ("Protestantse Kerk in Nederland", (string?)"PKN"),
                ("Nederlandse Gereformeerde Kerken", "NGK"),
                ("Gereformeerde Gemeenten", "GG"),
                ("Gereformeerde Bond", "GB"),
                ("Christelijke Gereformeerde Kerken", "CGK"),
                ("Hersteld Hervormd", "HHK") }),
            ("PreacherTitles", "Voorganger-titels", new[] {
                ("Ds.", (string?)null), ("Dr.", null), ("Prof.", null),
                ("Prof. dr.", null), ("Kand.", null), ("Ev.", null), ("Br.", null) }),
            ("SpecialOccasions", "Bijzonderheden", new[] {
                ("Avondmaal", (string?)null), ("Doop", null), ("Pasen", null),
                ("Pinksteren", null), ("Kerst", null), ("Biddag", null),
                ("Dankdag", null), ("Voorbereiding HA", null), ("Nabetrachting HA", null) }),
            ("ServicePerformer", "Wie doet het onderdeel", new[] {
                ("Voorganger", (string?)null), ("Ouderling", null), ("Gemeentelid", null) }),
            ("ServiceOccasion", "Aard van de dienst (voor sjablonen)", new[] {
                ("Regulier", (string?)null), ("Doop", null), ("Avondmaal", null),
                ("Belijdenis", null), ("Bevestiging ambtsdragers", null) }),
            ("BibleTranslations", "Bijbelvertalingen", new[] {
                ("Herziene Statenvertaling", (string?)"HSV"), ("Statenvertaling", "SV"),
                ("NBV21", "NBV21"), ("NBG 1951", "NBG") }),
            ("MusicalAccompaniment", "Muzikale begeleiding", new[] {
                ("Orgel", (string?)null), ("Piano", null), ("Band", null) }),
            ("ChurchCalendarSundays", "Zondagen kerkelijk jaar", new[] {
                ("Eerste Advent", (string?)null), ("Tweede Advent", null), ("Derde Advent", null),
                ("Vierde Advent", null), ("Kerst", null), ("Oudejaarsavond", null),
                ("Nieuwjaarsdag", null), ("Epifanie", null), ("Septuagesima", null),
                ("Sexagesima", null), ("Quinquagesima", null), ("Aswoensdag", null),
                ("Eerste na Trinitatis", null), ("Tweede na Trinitatis", null), ("Derde na Trinitatis", null),
                ("Goede Vrijdag", null), ("Stille Zaterdag", null), ("Eerste Paasdag", null),
                ("Tweede Paasdag", null), ("Hemelvaart", null), ("Eerste Pinksterdag", null),
                ("Tweede Pinksterdag", null), ("Trinitatis", null) }),
            ("LiturgicalLabels", "Liturgische labels", new[] {
                ("Muziek voor de dienst (orgel)", (string?)null),
                ("Muziek voor de dienst (anders)", null),
                ("Repertoire voor dienst (psalmen)", null),
                ("Repertoire voor dienst (psalmen/gezangen)", null),
                ("Repertoire voor dienst (psalmen/gezangen/orgelliteratuur)", null),
                ("Mededelingen", null), ("Votum", null), ("Groet", null),
                ("Groet (gebeden)", null), ("Vermaan/belijden", null),
                ("Schriftlezing(en)", null), ("Lector vermaan/belijden", null),
                ("Lector Schrift", null), ("Gebed om de opening van het Woord", null),
                ("Grote gebed (met voorbeden/dankzegging)", null), ("Dankgebed", null),
                ("Dankgebed (met voorbeden/dankzegging)", null), ("Preektekst", null),
                ("Thema preek", null), ("Voorzang", null), ("Openingslied", null),
                ("Na vermaan/belijden", null), ("Bij kindermoment", null),
                ("Voor de preek (zonder collecte)", null), ("Voor de preek (met collecte)", null),
                ("Tussenzang", null), ("Na de preek", null), ("Slotlied", null),
                ("Slotlied (met collecte)", null), ("Zegen", null), ("Zegen (gebeden)", null),
                ("Zegenbede", null), ("Na de zegen (orgelspel of...)", null),
                ("Collecte aan de deur", null), ("Overig", null) }),
        };

        var changed = false;
        foreach (var (name, description, items) in canonical)
        {
            var def = await db.ListDefinitions
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Name == name);
            if (def is null)
            {
                def = new ListDefinition { Id = Guid.NewGuid(), Name = name, Description = description, IsSystemList = true };
                db.ListDefinitions.Add(def);
                changed = true;
            }

            var isLabels = name == "LiturgicalLabels";
            for (var i = 0; i < items.Length; i++)
            {
                var (value, abbrev) = items[i];
                var existing = def.Items.FirstOrDefault(it => it.Value == value);
                if (existing is not null)
                {
                    // Backfill classification for labels seeded before this field existed.
                    if (isLabels && existing.LiturgicalElementType is null)
                    {
                        existing.LiturgicalElementType = ClassifyLabel(value);
                        changed = true;
                    }
                    continue;
                }
                var item = new ListItem
                {
                    Id = Guid.NewGuid(),
                    ListDefinitionId = def.Id,
                    Value = value,
                    Abbreviation = abbrev,
                    SortOrder = i + 1,
                    IsActive = true,
                    LiturgicalElementType = isLabels ? ClassifyLabel(value) : null,
                };
                def.Items.Add(item);
                db.ListItems.Add(item);
                changed = true;
            }
        }

        if (changed) await db.SaveChangesAsync();
    }

    /// <summary>
    /// Heuristic default classification of a liturgical label into an
    /// <see cref="ElementType"/>. Mirrors the frontend <c>elementTypeForLabel</c>
    /// heuristic; the value is editable per label on the Lijsten page.
    /// </summary>
    private static ElementType ClassifyLabel(string label)
    {
        var l = label.ToLowerInvariant();
        if (l.Contains("schriftlezing") || l.Contains("lezing")) return ElementType.Reading;
        if (l.Contains("gebed")) return ElementType.Prayer;
        if (l.Contains("lied") || l.Contains("zang") || l.Contains("psalm")) return ElementType.Song;
        if (l.Contains("votum") || l.Contains("groet") || l.Contains("zegen") || l.Contains("collecte") ||
            l.Contains("vermaan") || l.Contains("belijd") || l.Contains("mededeling") || l.Contains("muziek") ||
            l.Contains("kindermoment"))
        {
            return ElementType.LiturgicalAct;
        }
        return ElementType.Other;
    }

    private static async Task SeedSongCatalogAsync(ApplicationDbContext db, (ListItem Bundle, string Section, string ResourceFile)[] sources)
    {
        if (await db.Songs.AnyAsync()) return;

        var assembly = typeof(DataSeeder).Assembly;
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var (bundle, section, resourceFile) in sources)
        {
            var resourceName = $"LiturgiekStatistiek.Infrastructure.SeedData.{resourceFile}";
            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            var entries = System.Text.Json.JsonSerializer.Deserialize<List<SeedSong>>(json, options);
            if (entries is null) continue;

            foreach (var entry in entries.OrderBy(e => e.Number))
            {
                var song = new Song
                {
                    Id = Guid.NewGuid(),
                    BundleId = bundle.Id,
                    Section = section,
                    Number = entry.Number,
                    Title = entry.Title,
                    NumberOfVerses = entry.NumberOfVerses ?? entry.Verses?.Count
                };
                db.Songs.Add(song);

                if (entry.Verses is { Count: > 0 })
                {
                    var order = 0;
                    foreach (var v in entry.Verses)
                    {
                        db.SongCatalogVerses.Add(new SongCatalogVerse
                        {
                            Id = Guid.NewGuid(),
                            SongId = song.Id,
                            Number = v.Number,
                            Title = v.Title,
                            SortOrder = order++
                        });
                    }
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private sealed class SeedSong
    {
        public int Number { get; set; }
        public string? Title { get; set; }
        public int? NumberOfVerses { get; set; }
        public List<SeedVerse>? Verses { get; set; }
    }

    private sealed class SeedVerse
    {
        public int Number { get; set; }
        public string? Title { get; set; }
    }
}



