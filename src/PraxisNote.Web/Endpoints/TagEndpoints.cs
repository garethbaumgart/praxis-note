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
        group.MapGet("/{sourceId:guid}/merge-preview/{targetId:guid}", (Delegate)HandlePreviewMerge);
        group.MapPost("/{sourceId:guid}/merge-into/{targetId:guid}", (Delegate)HandleMergeTags);
    }

    private static async Task<IResult> HandleGetTags(
        HttpContext context,
        ClaimsPrincipal user,
        GetUserTags getUserTags,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var profileId = context.GetProfileId();
        var query = new GetUserTags.Query(userId.Value, profileId);
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
        HttpContext context,
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
            var profileId = context.GetProfileId();
            var command = new CreateTag.Command(userId.Value, profileId, request.Name);
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

    private static async Task<IResult> HandlePreviewMerge(
        Guid sourceId,
        Guid targetId,
        ClaimsPrincipal user,
        PreviewTagMerge previewTagMerge,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var query = new PreviewTagMerge.Query(userId.Value, sourceId, targetId);
            var result = await previewTagMerge.ExecuteAsync(query, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == PreviewTagMerge.SourceNotFoundError)
        {
            return Results.NotFound(new { error = "Source tag not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == PreviewTagMerge.TargetNotFoundError)
        {
            return Results.NotFound(new { error = "Target tag not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == PreviewTagMerge.SameTagError)
        {
            return Results.BadRequest(new { error = "Source and target tags must be different" });
        }
    }

    private static async Task<IResult> HandleMergeTags(
        Guid sourceId,
        Guid targetId,
        ClaimsPrincipal user,
        MergeTags mergeTags,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var command = new MergeTags.Command(userId.Value, sourceId, targetId);
            var result = await mergeTags.ExecuteAsync(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == MergeTags.SourceNotFoundError)
        {
            return Results.NotFound(new { error = "Source tag not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == MergeTags.TargetNotFoundError)
        {
            return Results.NotFound(new { error = "Target tag not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == MergeTags.SameTagError)
        {
            return Results.BadRequest(new { error = "Source and target tags must be different" });
        }
    }
}

public record CreateTagRequest(string Name);
public record UpdateTagRequest(string Name);
