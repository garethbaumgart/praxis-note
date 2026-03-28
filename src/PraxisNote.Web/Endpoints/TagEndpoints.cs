using System.Security.Claims;
using System.Text.Json;
using PraxisNote.Web.Extensions;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys;
using PraxisNote.Application.Features.UserAiKeys.Services;

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
        group.MapPost("/{id:guid}/chat", (Delegate)HandleChat);
        group.MapPost("/{id:guid}/starters", (Delegate)HandleGetStarters);
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

    private static async Task HandleChat(
        Guid id,
        HttpContext context,
        ClaimsPrincipal user,
        TagChatRequest request,
        AskTagAi askTagAi,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            context.Response.StatusCode = 401;
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Message is required\"}", cancellationToken);
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        try
        {
            var history = request.History?
                .Select(h => new ChatMessage(h.Role, h.Content))
                .ToList() ?? [];

            var command = new AskTagAi.Command(userId.Value, id, request.Message, history);

            await foreach (var token in askTagAi.ExecuteAsync(command, cancellationToken))
            {
                var tokenJson = JsonSerializer.Serialize(new { token });
                await context.Response.WriteAsync($"data: {tokenJson}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }

            await context.Response.WriteAsync("event: done\ndata: {}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message == AskTagAi.NotFoundError)
        {
            try
            {
                await context.Response.WriteAsync("event: error\ndata: {\"error\":\"Tag not found\"}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            catch { /* Client likely disconnected */ }
        }
        catch (InvalidOperationException ex) when (ex.Message == AskTagAi.NoContentError)
        {
            try
            {
                await context.Response.WriteAsync("event: error\ndata: {\"error\":\"This tag has no content to chat about\"}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            catch { /* Client likely disconnected */ }
        }
        catch (NoAiKeyConfiguredException)
        {
            try
            {
                await context.Response.WriteAsync($"event: error\ndata: {AiKeyErrorResults.NoAiKeySsePayload()}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            catch { /* Client likely disconnected */ }
        }
        catch (AiKeyInvalidException ex)
        {
            try
            {
                await context.Response.WriteAsync($"event: error\ndata: {AiKeyErrorResults.AiKeyInvalidSsePayload(ex)}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            catch { /* Client likely disconnected */ }
        }
        catch (AiRateLimitedException ex)
        {
            try
            {
                await context.Response.WriteAsync($"event: error\ndata: {AiKeyErrorResults.AiRateLimitedSsePayload(ex)}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            catch { /* Client likely disconnected */ }
        }
        catch (AiProviderException ex)
        {
            try
            {
                await context.Response.WriteAsync($"event: error\ndata: {AiKeyErrorResults.AiProviderErrorSsePayload(ex)}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            catch { /* Client likely disconnected */ }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during tag AI chat stream");
            try
            {
                await context.Response.WriteAsync("event: error\ndata: {\"error\":\"An error occurred while generating a response\"}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            catch { /* Client likely disconnected */ }
        }
    }

    private static async Task<IResult> HandleGetStarters(
        Guid id,
        ClaimsPrincipal user,
        GenerateTagStarters generateStarters,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var query = new GenerateTagStarters.Query(userId.Value, id);
            var starters = await generateStarters.ExecuteAsync(query, cancellationToken);

            return Results.Ok(new { starters });
        }
        catch (InvalidOperationException ex) when (ex.Message == GenerateTagStarters.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message == GenerateTagStarters.NoContentError)
        {
            return Results.Ok(new { starters = Array.Empty<string>() });
        }
        catch (NoAiKeyConfiguredException)
        {
            return AiKeyErrorResults.NoAiKeyResult();
        }
        catch (AiKeyInvalidException ex)
        {
            return AiKeyErrorResults.AiKeyInvalidResult(ex);
        }
        catch (AiRateLimitedException ex)
        {
            return AiKeyErrorResults.AiRateLimitedResult(ex);
        }
        catch (AiProviderException ex)
        {
            return AiKeyErrorResults.AiProviderErrorResult(ex);
        }
        catch (OperationCanceledException)
        {
            return Results.Ok(new { starters = Array.Empty<string>() });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error generating starter prompts for tag {TagId}", id);
            return Results.Ok(new { starters = Array.Empty<string>() });
        }
    }
}

public record CreateTagRequest(string Name);
public record UpdateTagRequest(string Name);
public record TagChatRequest(string Message, List<TagChatHistoryItem>? History);
public record TagChatHistoryItem(string Role, string Content);
