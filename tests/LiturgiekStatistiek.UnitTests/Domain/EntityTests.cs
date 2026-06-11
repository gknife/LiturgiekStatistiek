using LiturgiekStatistiek.Domain.Entities;
using NUnit.Framework;

namespace LiturgiekStatistiek.UnitTests.Domain;

[TestFixture]
public class EntityTests
{
    [Test]
    public void Service_NewInstance_HasEmptyCollections()
    {
        var service = new Service();
        Assert.That(service.Elements, Is.Not.Null);
        Assert.That(service.Elements, Is.Empty);
        Assert.That(service.Bundles, Is.Not.Null);
        Assert.That(service.Metadata, Is.Not.Null);
    }

    [Test]
    public void ServiceElement_NewInstance_HasEmptySongsCollection()
    {
        var element = new ServiceElement();
        Assert.That(element.Songs, Is.Not.Null);
        Assert.That(element.Songs, Is.Empty);
    }

    [Test]
    public void ServiceElementSong_NewInstance_HasEmptyVersesCollection()
    {
        var song = new ServiceElementSong();
        Assert.That(song.Verses, Is.Not.Null);
        Assert.That(song.Verses, Is.Empty);
    }

    [Test]
    public void Congregation_Defaults_AreCorrect()
    {
        var cong = new Congregation();
        Assert.That(cong.Name, Is.EqualTo(string.Empty));
        Assert.That(cong.City, Is.EqualTo(string.Empty));
        Assert.That(cong.Latitude, Is.Null);
        Assert.That(cong.Longitude, Is.Null);
        Assert.That(cong.Services, Is.Not.Null);
    }

    [Test]
    public void ListDefinition_HasItemsCollection()
    {
        var def = new ListDefinition();
        Assert.That(def.Items, Is.Not.Null);
        Assert.That(def.Items, Is.Empty);
    }

    [Test]
    public void ContentPage_Defaults()
    {
        var page = new ContentPage();
        Assert.That(page.Slug, Is.EqualTo(string.Empty));
        Assert.That(page.TitleNl, Is.EqualTo(string.Empty));
        Assert.That(page.ContentMarkdown, Is.EqualTo(string.Empty));
    }
}
