using LiturgiekStatistiek.Application.Services;
using NUnit.Framework;

namespace LiturgiekStatistiek.UnitTests.Services;

[TestFixture]
public class SongCompletenessCalculatorTests
{
    [Test]
    public void ParseVerseNumbers_ExpandsRanges_AndIgnoresNonNumeric()
    {
        var result = SongCompletenessCalculator.ParseVerseNumbers(new[] { "1", "2-4", "refrein", "6a" });

        Assert.That(result, Is.EquivalentTo(new[] { 1, 2, 3, 4, 6 }));
    }

    [Test]
    public void Compute_AllVersesInOneElement_IsCompleteInElementAndService()
    {
        var comp = SongCompletenessCalculator.Compute(
            catalogVerseCount: 4,
            elementVerseLabels: new[] { "1", "2", "3", "4" },
            serviceVerseLabels: new[] { "1", "2", "3", "4" });

        Assert.Multiple(() =>
        {
            Assert.That(comp.CompleteInElement, Is.True);
            Assert.That(comp.CompleteInService, Is.True);
            Assert.That(comp.State, Is.EqualTo(SongCompletenessCalculator.StateCompleteElement));
        });
    }

    [Test]
    public void Compute_VersesSplitAcrossElements_IsCompleteInServiceOnly()
    {
        // 93:1,2 in one onderdeel and 93:3,4 in another → whole-service complete.
        var comp = SongCompletenessCalculator.Compute(
            catalogVerseCount: 4,
            elementVerseLabels: new[] { "1", "2" },
            serviceVerseLabels: new[] { "1", "2", "3", "4" });

        Assert.Multiple(() =>
        {
            Assert.That(comp.CompleteInElement, Is.False);
            Assert.That(comp.CompleteInService, Is.True);
            Assert.That(comp.State, Is.EqualTo(SongCompletenessCalculator.StateCompleteService));
        });
    }

    [Test]
    public void Compute_MissingVerses_IsIncomplete()
    {
        var comp = SongCompletenessCalculator.Compute(
            catalogVerseCount: 5,
            elementVerseLabels: new[] { "1", "3" },
            serviceVerseLabels: new[] { "1", "3" });

        Assert.Multiple(() =>
        {
            Assert.That(comp.CompleteInElement, Is.False);
            Assert.That(comp.CompleteInService, Is.False);
            Assert.That(comp.State, Is.EqualTo(SongCompletenessCalculator.StateIncomplete));
        });
    }

    [Test]
    public void Compute_UnknownCatalogCount_IsUnknown_AndNeverComplete()
    {
        var comp = SongCompletenessCalculator.Compute(
            catalogVerseCount: null,
            elementVerseLabels: new[] { "1", "2", "3" },
            serviceVerseLabels: new[] { "1", "2", "3" });

        Assert.Multiple(() =>
        {
            Assert.That(comp.State, Is.EqualTo(SongCompletenessCalculator.StateUnknown));
            Assert.That(comp.CompleteInElement, Is.False);
            Assert.That(comp.CompleteInService, Is.False);
        });
    }

    [Test]
    public void Compute_SungInFull_IsComplete_EvenWhenCatalogUnknown()
    {
        var comp = SongCompletenessCalculator.Compute(
            catalogVerseCount: null,
            elementVerseLabels: new[] { "1" },
            serviceVerseLabels: new[] { "1" },
            sungInFull: true);

        Assert.Multiple(() =>
        {
            Assert.That(comp.State, Is.EqualTo(SongCompletenessCalculator.StateCompleteElement));
            Assert.That(comp.CompleteInElement, Is.True);
            Assert.That(comp.CompleteInService, Is.True);
        });
    }

    [Test]
    public void Compute_SungInFull_OverridesIncompleteVerseSet()
    {
        // Only verse 1 of a 5-verse song, but explicitly marked "hele lied".
        var comp = SongCompletenessCalculator.Compute(
            catalogVerseCount: 5,
            elementVerseLabels: new[] { "1" },
            serviceVerseLabels: new[] { "1" },
            sungInFull: true);

        Assert.Multiple(() =>
        {
            Assert.That(comp.State, Is.EqualTo(SongCompletenessCalculator.StateCompleteElement));
            Assert.That(comp.CompleteInElement, Is.True);
            Assert.That(comp.CompleteInService, Is.True);
        });
    }
}
