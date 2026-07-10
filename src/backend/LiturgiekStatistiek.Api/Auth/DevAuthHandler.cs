using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiturgiekStatistiek.Api.Auth;

/// <summary>
/// Development-only authentication handler used when "DisableAuthentication" is set.
/// Authenticates every request as a fixed local editor so the app (which now gates
/// all mutating endpoints behind [Authorize]) is fully usable without Entra ID.
/// </summary>
public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevBypass";

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "dev@localhost"),
            new Claim(ClaimTypes.NameIdentifier, "dev-user"),
            // Friendly display name, mirrors the frontend devBypass user so that
            // audit "who" fields match the name shown in the header ("Ontwikkelaar").
            new Claim("name", "Ontwikkelaar"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
