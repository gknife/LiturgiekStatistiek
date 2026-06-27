using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Infrastructure.Services;
using NUnit.Framework;

namespace LiturgiekStatistiek.UnitTests.Services;

[TestFixture]
public class LiturgyParserTests
{
    private LiturgyParser _parser = null!;

    [SetUp]
    public void SetUp() => _parser = new LiturgyParser();

    [Test]
    public void Parse_SongReference_ExtractsBundleNumberAndVerses()
    {
        var result = _parser.Parse("Ps. 32 : 1 en 3");

        Assert.That(result.Elements, Has.Count.EqualTo(1));
        var song = result.Elements[0];
        Assert.Multiple(() =>
        {
            Assert.That(song.SongBundle, Is.EqualTo("Ps1773"));
            Assert.That(song.SongNumber, Is.EqualTo(32));
            Assert.That(song.Verses, Is.EqualTo(new[] { "1", "3" }));
        });
    }

    [Test]
    public void Parse_VerseList_IsParsed()
    {
        var result = _parser.Parse("LvdK 91: 1, 2, 4");

        var song = result.Elements[0];
        Assert.Multiple(() =>
        {
            Assert.That(song.SongBundle, Is.EqualTo("LvdK"));
            Assert.That(song.SongNumber, Is.EqualTo(91));
            Assert.That(song.Verses, Is.EqualTo(new[] { "1", "2", "4" }));
        });
    }

    [Test]
    public void Parse_DashRange_ExpandsAllVerses()
    {
        var result = _parser.Parse("Opw. 220 : 1-3");

        var song = result.Elements[0];
        Assert.Multiple(() =>
        {
            Assert.That(song.SongBundle, Is.EqualTo("Opw"));
            Assert.That(song.SongNumber, Is.EqualTo(220));
            Assert.That(song.Verses, Is.EqualTo(new[] { "1", "2", "3" }));
        });
    }

    [Test]
    public void Parse_PreektekstLine_SetsSermonText()
    {
        var result = _parser.Parse("Tekst : HC zondag 40");

        Assert.That(result.SermonText, Is.EqualTo("HC zondag 40"));
        Assert.That(result.Elements.Exists(e => e.Label == "Preektekst"));
    }

    [Test]
    public void Parse_SchriftlezingLine_CreatesReadingElement()
    {
        var result = _parser.Parse("Schriftlezing : 1 Johannes 3");

        var reading = result.Elements.Find(e => e.Label == "Schriftlezing(en)");
        Assert.That(reading, Is.Not.Null);
        Assert.That(reading!.Notes, Is.EqualTo("1 Johannes 3"));
    }

    [Test]
    public void Parse_FirstAndLastUnlabeledSongs_BecomeOpeningsliedAndSlotlied()
    {
        var result = _parser.Parse("Ps. 100 : 1\nPs. 8 : 2\nPs. 150 : 1");

        Assert.Multiple(() =>
        {
            Assert.That(result.Elements[0].Label, Is.EqualTo("Openingslied"));
            Assert.That(result.Elements[^1].Label, Is.EqualTo("Slotlied"));
        });
    }

    [Test]
    public void Parse_KerkdienstgemistSample_ExtractsReadingSermonAndSongs()
    {
        const string text =
            "Schriftlezing : 1 Johannes 3 / Tekst : HC zondag 40 / Ps. 32 : 1 en 3 / Opw. 220";

        var result = _parser.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.SermonText, Is.EqualTo("HC zondag 40"));
            Assert.That(result.Elements.Exists(e => e.Label == "Schriftlezing(en)" && e.Notes == "1 Johannes 3"));
            Assert.That(result.Elements.Exists(e => e.SongBundle == "Ps1773" && e.SongNumber == 32));
            Assert.That(result.Elements.Exists(e => e.SongBundle == "Opw" && e.SongNumber == 220));
        });
    }

    [Test]
    public void Parse_Title_ExtractsPreacherDateAndTimeOfDay()
    {
        var result = _parser.Parse(string.Empty, "ds. J. de Vries - 27 juni - 09:30");

        Assert.Multiple(() =>
        {
            Assert.That(result.Preacher, Does.Contain("de Vries"));
            Assert.That(result.Date, Is.Not.Null);
            Assert.That(result.TimeOfDay, Is.EqualTo("Morning"));
        });
    }

    [Test]
    public void Parse_AfternoonTime_MapsToAfternoon()
    {
        var result = _parser.Parse(string.Empty, "ds. A. Bakker - 14:30");
        Assert.That(result.TimeOfDay, Is.EqualTo("Afternoon"));
    }

    [Test]
    public void Parse_EveningTime_MapsToEvening()
    {
        var result = _parser.Parse(string.Empty, "ds. A. Bakker - 19:00");
        Assert.That(result.TimeOfDay, Is.EqualTo("Evening"));
    }

    [Test]
    public void Parse_LabeledLines_MapKeywordsToCanonicalLabels()
    {
        var result = _parser.Parse("Votum\nGroet\nDankgebed\nZegen");

        Assert.Multiple(() =>
        {
            Assert.That(result.Elements.Exists(e => e.Label == "Votum"));
            Assert.That(result.Elements.Exists(e => e.Label == "Groet"));
            Assert.That(result.Elements.Exists(e => e.Label == "Dankgebed"));
            Assert.That(result.Elements.Exists(e => e.Label == "Zegen"));
        });
    }

    [Test]
    public void Parse_EmptyText_ReturnsNoElements()
    {
        var result = _parser.Parse(string.Empty);
        Assert.That(result.Elements, Is.Empty);
    }
}
