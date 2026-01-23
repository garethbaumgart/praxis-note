using System.Security.Claims;

namespace PraxisNote.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the user ID from the ClaimsPrincipal's NameIdentifier claim.
    /// </summary>
    /// <param name="user">The claims principal representing the authenticated user.</param>
    /// <returns>The user's GUID if valid, otherwise null.</returns>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}
