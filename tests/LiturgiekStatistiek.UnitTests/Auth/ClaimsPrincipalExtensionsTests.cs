using System.Security.Claims;
using LiturgiekStatistiek.Api.Auth;
using NUnit.Framework;

namespace LiturgiekStatistiek.UnitTests.Auth;

[TestFixture]
public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "test"));

    [Test]
    public void GetDisplayName_PrefersNameClaim_OverEmailIdentity()
    {
        // Under Entra ID Identity.Name is typically the email/UPN, but the header
        // shows the friendly "name" claim; audit "who" must match the header.
        var user = Principal(
            new Claim("name", "Jan de Vries"),
            new Claim(ClaimTypes.Name, "jan.devries@example.com"));

        Assert.That(user.GetDisplayName(), Is.EqualTo("Jan de Vries"));
    }

    [Test]
    public void GetDisplayName_FallsBackToGivenName_WhenNoNameClaim()
    {
        var user = Principal(
            new Claim(ClaimTypes.GivenName, "Piet"),
            new Claim(ClaimTypes.Name, "piet@example.com"));

        Assert.That(user.GetDisplayName(), Is.EqualTo("Piet"));
    }

    [Test]
    public void GetDisplayName_FallsBackToIdentityName_WhenNoFriendlyClaim()
    {
        var user = Principal(new Claim(ClaimTypes.Name, "someone@example.com"));

        Assert.That(user.GetDisplayName(), Is.EqualTo("someone@example.com"));
    }

    [Test]
    public void GetDisplayName_ReturnsUnknown_WhenNoClaims()
    {
        var user = Principal();

        Assert.That(user.GetDisplayName(), Is.EqualTo("unknown"));
    }

    [Test]
    public void GetDisplayName_ReturnsUnknown_WhenNull()
    {
        ClaimsPrincipal? user = null;

        Assert.That(user.GetDisplayName(), Is.EqualTo("unknown"));
    }
}
