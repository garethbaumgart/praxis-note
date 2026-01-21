using System.Security.Claims;
using PraxisNote.Application.Features.Tags;

namespace PraxisNote.Web.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tags")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetTags);
        group.MapPost("/", (Delegate)HandleCreateTag);
        group.MapPut("/{id:guid}", (Delegate)HandleUpdateTag);
        group.MapDelete("/{id:guid}", (Delegate)HandleDeleteTag);
    }

    private static async Task<IResult> HandleGetTags(
        ClaimsPrincipal user,
        GetUserTags getUserTags,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserTags.Query(userId.Value);
        var tags = await getUserTags.ExecuteAsync(query, cancellationToken);

        return Results.Ok(tags);
    }

    private static async Task<IResult> HandleCreateTag(
        ClaimsPrincipal user,
        CreateTagRequest request,
        CreateTag createTag,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Color))
        {
            return Results.BadRequest(new { error = "Color is required" });
        }

        var command = new CreateTag.Command(userId.Value, request.Name, request.Color);
        var result = await createTag.ExecuteAsync(command, cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return Results.Created($"/api/tags/{result.TagId}", new { id = result.TagId });
    }

    private static async Task<IResult> HandleUpdateTag(
        Guid id,
        ClaimsPrincipal user,
        UpdateTagRequest request,
        UpdateTag updateTag,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateTag.Command(id, userId.Value, request.Name, request.Color);
        var result = await updateTag.ExecuteAsync(command, cancellationToken);

        if (!result.Success)
        {
            return result.Error == "Tag not found"
                ? Results.NotFound()
                : Results.BadRequest(new { error = result.Error });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> HandleDeleteTag(
        Guid id,
        ClaimsPrincipal user,
        DeleteTag deleteTag,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var command = new DeleteTag.Command(id, userId.Value);
        var success = await deleteTag.ExecuteAsync(command, cancellationToken);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}

public record CreateTagRequest(string Name, string Color);
public record UpdateTagRequest(string? Name, string? Color);
