using System.Security.Claims;

namespace PraxisNote.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the user ID from the ClaimsPrincipal's NameIdentifier claim.
    /// </summary>
    /// <param name="user">The claims principal representing the authenticated user.</param>
    /// <returns>
    /// The user's GUID if the NameIdentifier claim exists and is a valid GUID.
    /// Returns null if the claim is missing, empty, or not a valid GUID format.
    /// Callers should treat null as an unauthorized request.
    /// </returns>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }

    /// <summary>
    /// Extracts the user's display name from the ClaimsPrincipal's Name claim.
    /// </summary>
    /// <param name="user">The claims principal representing the authenticated user.</param>
    /// <returns>
    /// The user's display name if the Name claim exists, or null if missing.
    /// </returns>
    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Name);
    }
}
