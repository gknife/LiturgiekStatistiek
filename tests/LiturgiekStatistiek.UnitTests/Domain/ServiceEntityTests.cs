using FluentAssertions;
using LiturgiekStatistiek.Domain.Entities;

namespace LiturgiekStatistiek.UnitTests.Domain;

[TestFixture]
public class ServiceEntityTests
{
    [Test]
    public void Service_NewInstance_HasEmptyCollections()
    {
        var service = new Service();

        service.Elements.Should().BeEmpty();
        service.Bundles.Should().BeEmpty();
        service.Metadata.Should().BeEmpty();
    }

    [Test]
    public void ServiceElement_NewInstance_HasEmptySongsCollection()
    {
        var element = new ServiceElement();

        element.Songs.Should().BeEmpty();
    }

    [Test]
    public void ServiceElementSong_NewInstance_HasEmptyVersesCollection()
    {
        var song = new ServiceElementSong();

        song.Verses.Should().BeEmpty();
    }
}
