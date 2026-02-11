namespace PraxisNote.Web.Extensions;

public static class HttpContextExtensions
{
    private const string ProfileIdKey = "ProfileId";

    /// <summary>
    /// Gets the validated profile ID from HttpContext.Items, set by ProfileValidationMiddleware.
    /// </summary>
    public static Guid GetProfileId(this HttpContext context)
    {
        if (context.Items.TryGetValue(ProfileIdKey, out var value) && value is Guid profileId)
        {
            return profileId;
        }

        throw new InvalidOperationException(
            "ProfileId not found in HttpContext.Items. Ensure ProfileValidationMiddleware is registered.");
    }

    /// <summary>
    /// Sets the profile ID in HttpContext.Items. Called by ProfileValidationMiddleware.
    /// </summary>
    internal static void SetProfileId(this HttpContext context, Guid profileId)
    {
        context.Items[ProfileIdKey] = profileId;
    }
}
