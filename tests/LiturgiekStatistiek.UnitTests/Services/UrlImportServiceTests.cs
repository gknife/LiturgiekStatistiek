using System;
using LiturgiekStatistiek.Infrastructure.Services;
using NUnit.Framework;

namespace LiturgiekStatistiek.UnitTests.Services;

[TestFixture]
public class UrlImportServiceTests
{
    [Test]
    public void ParseKerkdienstgemist_SimpleEveningService_ExtractsDateChurchAndCity()
    {
        var uri = new Uri("https://kerkdienstgemist.nl/stations/1060/events/recording/178326870001060");
        var data = UrlImportService.ParseKerkdienstgemist(
            uri,
            title: "Avonddienst - Hervormde gemeente Ederveen",
            description: "Kerkdienst Hervormde gemeente Ederveen");

        Assert.Multiple(() =>
        {
            Assert.That(data.Date, Is.EqualTo("2026-07-05"));
            Assert.That(data.TimeOfDay, Is.EqualTo("Evening"));
            Assert.That(data.Congregation, Is.EqualTo("Hervormde gemeente"));
            Assert.That(data.City, Is.EqualTo("Ederveen"));
            Assert.That(data.Preacher, Is.Null);
        });
    }

    [Test]
    public void ParseKerkdienstgemist_PreacherThemeAndChurch_ExtractsAllParts()
    {
        var uri = new Uri("https://kerkdienstgemist.nl/stations/95/events/recording/178326900000095");
        var data = UrlImportService.ParseKerkdienstgemist(
            uri,
            title: "ds. A. van der Stoep (Wapenveld) - Saulus wordt een bidder en een broeder - Hervormde Wijkgemeente van bijzondere aard Eben-Haëzerkerk Apeldoorn",
            description: "ds. A. van der Stoep (Wapenveld) - Handelingen 9: 1-22");

        Assert.Multiple(() =>
        {
            Assert.That(data.Date, Is.EqualTo("2026-07-05"));
            Assert.That(data.Preacher, Is.EqualTo("ds. A. van der Stoep (Wapenveld)"));
            Assert.That(data.SermonTheme, Is.EqualTo("Saulus wordt een bidder en een broeder"));
            Assert.That(data.City, Is.EqualTo("Apeldoorn"));
            Assert.That(data.Congregation, Does.StartWith("Hervormde Wijkgemeente"));
            Assert.That(data.SermonText, Is.EqualTo("Handelingen 9: 1-22"));
        });
    }

    [Test]
    public void ParseKerkdienstgemist_RecordingIdTimestamp_MatchesKnownService()
    {
        // 177117660000093 -> unix 1771176600 -> 2026-02-15 (an Avond service).
        var uri = new Uri("https://kerkdienstgemist.nl/stations/93/events/recording/177117660000093");
        var data = UrlImportService.ParseKerkdienstgemist(uri, title: null, description: null);

        Assert.Multiple(() =>
        {
            Assert.That(data.Date, Is.EqualTo("2026-02-15"));
            Assert.That(data.TimeOfDay, Is.EqualTo("Evening"));
        });
    }
}
