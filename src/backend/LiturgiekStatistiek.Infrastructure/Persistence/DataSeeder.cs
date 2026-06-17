using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.ListDefinitions.AnyAsync()) return;

        // --- Song Bundles ---
        var bundleList = new ListDefinition { Id = Guid.NewGuid(), Name = "SongBundles", Description = "Liedbundels", IsSystemList = true };
        db.ListDefinitions.Add(bundleList);

        var ps1773 = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Psalmen 1773", Abbreviation = "Ps1773", SortOrder = 1 };
        var psOB = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Psalmen Onberijmd", Abbreviation = "PsOB", SortOrder = 2 };
        var lvdK = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Liedboek voor de Kerken", Abbreviation = "LvdK", SortOrder = 3 };
        var wk = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Weerklank", Abbreviation = "WK", SortOrder = 4 };
        var wkPs = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Weerklank Psalmen", Abbreviation = "WKPs", SortOrder = 5 };
        var opw = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Opwekking", Abbreviation = "Opw", SortOrder = 6 };
        var gk = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Gereformeerd Kerkboek", Abbreviation = "GK", SortOrder = 7 };
        var eg = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bundleList.Id, Value = "Evangelische Gezangen", Abbreviation = "EG", SortOrder = 8 };
        db.ListItems.AddRange(ps1773, psOB, lvdK, wk, wkPs, opw, gk, eg);

        // --- Denominations ---
        var denomList = new ListDefinition { Id = Guid.NewGuid(), Name = "Denominations", Description = "Kerkgenootschappen", IsSystemList = true };
        db.ListDefinitions.Add(denomList);
        var pkn = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = denomList.Id, Value = "Protestantse Kerk in Nederland", Abbreviation = "PKN", SortOrder = 1 };
        var ngk = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = denomList.Id, Value = "Nederlandse Gereformeerde Kerken", Abbreviation = "NGK", SortOrder = 2 };
        var gg = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = denomList.Id, Value = "Gereformeerde Gemeenten", Abbreviation = "GG", SortOrder = 3 };
        var gb = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = denomList.Id, Value = "Gereformeerde Bond", Abbreviation = "GB", SortOrder = 4 };
        var cgk = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = denomList.Id, Value = "Christelijke Gereformeerde Kerken", Abbreviation = "CGK", SortOrder = 5 };
        var hh = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = denomList.Id, Value = "Hersteld Hervormd", Abbreviation = "HHK", SortOrder = 6 };
        db.ListItems.AddRange(pkn, ngk, gg, gb, cgk, hh);

        // --- Special Occasions ---
        var occasionList = new ListDefinition { Id = Guid.NewGuid(), Name = "SpecialOccasions", Description = "Bijzonderheden", IsSystemList = true };
        db.ListDefinitions.Add(occasionList);
        var avondmaal = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Avondmaal", SortOrder = 1 };
        var doop = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Doop", SortOrder = 2 };
        var pasen = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Pasen", SortOrder = 3 };
        var pinksteren = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Pinksteren", SortOrder = 4 };
        var kerst = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Kerst", SortOrder = 5 };
        var biddag = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Biddag", SortOrder = 6 };
        var dankdag = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Dankdag", SortOrder = 7 };
        var voorbHA = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Voorbereiding HA", SortOrder = 8 };
        var nabetHA = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = occasionList.Id, Value = "Nabetrachting HA", SortOrder = 9 };
        db.ListItems.AddRange(avondmaal, doop, pasen, pinksteren, kerst, biddag, dankdag, voorbHA, nabetHA);

        // --- Bible Translations ---
        var bibleList = new ListDefinition { Id = Guid.NewGuid(), Name = "BibleTranslations", Description = "Bijbelvertalingen", IsSystemList = true };
        db.ListDefinitions.Add(bibleList);
        var hsv = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bibleList.Id, Value = "Herziene Statenvertaling", Abbreviation = "HSV", SortOrder = 1 };
        var sv = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bibleList.Id, Value = "Statenvertaling", Abbreviation = "SV", SortOrder = 2 };
        var nbv21 = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bibleList.Id, Value = "NBV21", Abbreviation = "NBV21", SortOrder = 3 };
        var nbg = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = bibleList.Id, Value = "NBG 1951", Abbreviation = "NBG", SortOrder = 4 };
        db.ListItems.AddRange(hsv, sv, nbv21, nbg);

        // --- Musical Accompaniment ---
        var musicList = new ListDefinition { Id = Guid.NewGuid(), Name = "MusicalAccompaniment", Description = "Muzikale begeleiding", IsSystemList = true };
        db.ListDefinitions.Add(musicList);
        var orgel = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = musicList.Id, Value = "Orgel", SortOrder = 1 };
        var piano = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = musicList.Id, Value = "Piano", SortOrder = 2 };
        var band = new ListItem { Id = Guid.NewGuid(), ListDefinitionId = musicList.Id, Value = "Band", SortOrder = 3 };
        db.ListItems.AddRange(orgel, piano, band);

        // --- Church Calendar ---
        var calList = new ListDefinition { Id = Guid.NewGuid(), Name = "ChurchCalendarSundays", Description = "Zondagen kerkelijk jaar", IsSystemList = true };
        db.ListDefinitions.Add(calList);
        var calItems = new[] { "Eerste Advent", "Tweede Advent", "Derde Advent", "Vierde Advent",
            "Kerst", "Oudejaarsavond", "Nieuwjaarsdag", "Epifanie",
            "Septuagesima", "Sexagesima", "Quinquagesima", "Aswoensdag",
            "Eerste na Trinitatis", "Tweede na Trinitatis", "Derde na Trinitatis",
            "Goede Vrijdag", "Stille Zaterdag", "Eerste Paasdag", "Tweede Paasdag",
            "Hemelvaart", "Eerste Pinksterdag", "Tweede Pinksterdag", "Trinitatis" };
        for (int i = 0; i < calItems.Length; i++)
            db.ListItems.Add(new ListItem { Id = Guid.NewGuid(), ListDefinitionId = calList.Id, Value = calItems[i], SortOrder = i + 1 });

        // --- Liturgical Labels ---
        var labelList = new ListDefinition { Id = Guid.NewGuid(), Name = "LiturgicalLabels", Description = "Liturgische labels", IsSystemList = true };
        db.ListDefinitions.Add(labelList);
        var labels = new[] { "Voorzang", "Openingslied", "Na vermaan/belijden", "Bij kindermoment",
            "Voor de preek", "Tussenzang", "Na de preek", "Slotlied", "Na de zegen",
            "Votum", "Groet", "Vermaan/belijden", "Schriftlezing", "Gebed", "Preek",
            "Mededelingen", "Collecte", "Zegen", "Geloofsbelijdenis", "Dankgebed" };
        for (int i = 0; i < labels.Length; i++)
            db.ListItems.Add(new ListItem { Id = Guid.NewGuid(), ListDefinitionId = labelList.Id, Value = labels[i], SortOrder = i + 1 });

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
        var preacher1 = new Preacher { Id = Guid.NewGuid(), FullName = "ds. Janneke Dekker", Title = "ds." };
        var preacher2 = new Preacher { Id = Guid.NewGuid(), FullName = "ds. R.A.M. Visser", Title = "ds." };
        var preacher3 = new Preacher { Id = Guid.NewGuid(), FullName = "prop. J. van der Knijff", Title = "prop." };
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
        var label_voorzang = db.ListItems.Local.FirstOrDefault(l => l.Value == "Voorzang");
        var label_opening = db.ListItems.Local.FirstOrDefault(l => l.Value == "Openingslied");
        var label_napreek = db.ListItems.Local.FirstOrDefault(l => l.Value == "Na de preek");
        var label_slotlied = db.ListItems.Local.FirstOrDefault(l => l.Value == "Slotlied");
        var label_zegen = db.ListItems.Local.FirstOrDefault(l => l.Value == "Zegen");

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

        var s2_1 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_1.Id, BundleId = ps1773.Id, SongNumber = 63, Position = 1 };
        db.ServiceElementSongs.Add(s2_1);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_1.Id, VerseLabel = "2", Position = 1 });

        var s2_2 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_2.Id, BundleId = ps1773.Id, SongNumber = 143, Position = 1 };
        db.ServiceElementSongs.Add(s2_2);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_2.Id, VerseLabel = "2", Position = 1 });

        var s2_3 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_3.Id, BundleId = ps1773.Id, SongNumber = 145, Position = 1 };
        db.ServiceElementSongs.Add(s2_3);
        db.SongVerses.AddRange(
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_3.Id, VerseLabel = "2", Position = 1 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_3.Id, VerseLabel = "4", Position = 2 },
            new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_3.Id, VerseLabel = "6", Position = 3 }
        );

        var s2_4 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_4.Id, BundleId = ps1773.Id, SongNumber = 146, Position = 1 };
        db.ServiceElementSongs.Add(s2_4);
        db.SongVerses.Add(new SongVerse { Id = Guid.NewGuid(), ServiceElementSongId = s2_4.Id, VerseLabel = "6", Position = 1 });

        var s2_5 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el2_5.Id, BundleId = ps1773.Id, SongNumber = 18, Position = 1 };
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

        // Voorzang: EG 9:1,7 + WK 73:1
        var s3_1a = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_1.Id, BundleId = eg.Id, SongNumber = 9, Position = 1 };
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

        // Voor de preek: PsOB 133:1,2,3
        var s3_4 = new ServiceElementSong { Id = Guid.NewGuid(), ServiceElementId = el3_4.Id, BundleId = psOB.Id, SongNumber = 133, Position = 1 };
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

        // --- Content pages ---
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

        await db.SaveChangesAsync();

        await SeedSongCatalogAsync(db, ps1773);
    }

    private static async Task SeedSongCatalogAsync(ApplicationDbContext db, ListItem ps1773Bundle)
    {
        if (await db.Songs.AnyAsync()) return;

        var assembly = typeof(DataSeeder).Assembly;
        var resourceName = "LiturgiekStatistiek.Infrastructure.SeedData.psalmen-1773.json";
        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return;

        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        var json = await reader.ReadToEndAsync();
        var entries = System.Text.Json.JsonSerializer.Deserialize<List<SeedSong>>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (entries is null) return;

        foreach (var entry in entries.OrderBy(e => e.Number))
        {
            db.Songs.Add(new Song
            {
                Id = Guid.NewGuid(),
                BundleId = ps1773Bundle.Id,
                Number = entry.Number,
                Title = entry.Title,
                NumberOfVerses = entry.NumberOfVerses
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed class SeedSong
    {
        public int Number { get; set; }
        public string? Title { get; set; }
        public int? NumberOfVerses { get; set; }
    }
}



