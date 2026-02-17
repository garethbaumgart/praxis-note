using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Mcp;

public sealed class McpUserContext(IHttpContextAccessor httpContextAccessor)
{
    public Guid UserId => httpContextAccessor.HttpContext?.User.GetUserId()
        ?? throw new UnauthorizedAccessException("No authenticated user");

    public Guid ProfileId => httpContextAccessor.HttpContext?.GetProfileId()
        ?? throw new InvalidOperationException("No profile set");
}
