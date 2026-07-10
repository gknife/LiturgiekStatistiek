using System.Security.Claims;

namespace LiturgiekStatistiek.Api.Auth;

/// <summary>
/// Helpers for reading a friendly identity from the signed-in principal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the user's display name for audit "who" fields. Under Entra ID
    /// <see cref="ClaimsIdentity.Name"/> typically resolves to the email/UPN
    /// (preferred_username), which is not what the UI shows in the header. The
    /// header uses the token's "name" claim (the friendly display name), so audit
    /// records must prefer that same claim and only fall back to the name/UPN.
    /// </summary>
    public static string GetDisplayName(this ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return "unknown";
        }

        var displayName = user.FindFirstValue("name")
            ?? user.FindFirstValue(ClaimTypes.GivenName)
            ?? user.Identity?.Name;

        return string.IsNullOrWhiteSpace(displayName) ? "unknown" : displayName;
    }
}
