using System.Security.Claims;
using PraxisNote.Web.Extensions;
using PraxisNote.Application.Features.Tags;

namespace PraxisNote.Web.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tags")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetTags);
        group.MapGet("/{id:guid}/items", (Delegate)HandleGetItemsByTag);
        group.MapPost("/", (Delegate)HandleCreateTag);
        group.MapPut("/{id:guid}", (Delegate)HandleUpdateTag);
        group.MapDelete("/{id:guid}", (Delegate)HandleDeleteTag);
    }

    private static async Task<IResult> HandleGetTags(
        ClaimsPrincipal user,
        GetUserTags getUserTags,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetUserTags.Query(userId.Value);
        var tags = await getUserTags.ExecuteAsync(query, cancellationToken);

        return Results.Ok(tags);
    }

    private static async Task<IResult> HandleGetItemsByTag(
        Guid id,
        ClaimsPrincipal user,
        GetItemsByTag getItemsByTag,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var query = new GetItemsByTag.Query(userId.Value, id);
            var result = await getItemsByTag.ExecuteAsync(query, cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == GetItemsByTag.NotFoundError)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> HandleCreateTag(
        ClaimsPrincipal user,
        CreateTagRequest request,
        CreateTag createTag,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        try
        {
            var command = new CreateTag.Command(userId.Value, request.Name);
            var result = await createTag.ExecuteAsync(command, cancellationToken);

            return Results.Created($"/api/tags/{result.TagId}", new { id = result.TagId });
        }
        catch (InvalidOperationException ex) when (ex.Message == CreateTag.DuplicateNameError)
        {
            return Results.Conflict(new { error = "A tag with this name already exists" });
        }
    }

    private static async Task<IResult> HandleUpdateTag(
        Guid id,
        ClaimsPrincipal user,
        UpdateTagRequest request,
        UpdateTag updateTag,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        try
        {
            var command = new UpdateTag.Command(userId.Value, id, request.Name);
            await updateTag.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == UpdateTag.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message == UpdateTag.DuplicateNameError)
        {
            return Results.Conflict(new { error = "A tag with this name already exists" });
        }
    }

    private static async Task<IResult> HandleDeleteTag(
        Guid id,
        ClaimsPrincipal user,
        DeleteTag deleteTag,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var command = new DeleteTag.Command(userId.Value, id);
            await deleteTag.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == DeleteTag.NotFoundError)
        {
            return Results.NotFound();
        }
    }
}

public record CreateTagRequest(string Name);
public record UpdateTagRequest(string Name);
