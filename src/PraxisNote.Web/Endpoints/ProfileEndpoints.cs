using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.Profiles;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/profiles")
            .RequireAuthorization();

        group.MapGet("/", (Delegate)HandleGetProfiles);
        group.MapPost("/", (Delegate)HandleCreateProfile);
        group.MapPut("/{id:guid}", (Delegate)HandleUpdateProfile);
        group.MapDelete("/{id:guid}", (Delegate)HandleDeleteProfile);
        group.MapPost("/{id:guid}/default", (Delegate)HandleSetDefault);
    }

    private static async Task<IResult> HandleGetProfiles(
        ClaimsPrincipal user,
        [FromServices] GetUserProfiles getUserProfiles,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var query = new GetUserProfiles.Query(userId.Value);
        var profiles = await getUserProfiles.ExecuteAsync(query, cancellationToken);

        return Results.Ok(profiles);
    }

    private static async Task<IResult> HandleCreateProfile(
        ClaimsPrincipal user,
        CreateProfileRequest request,
        [FromServices] CreateProfile createProfile,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        try
        {
            var command = new CreateProfile.Command(userId.Value, request.Name, request.Icon);
            var result = await createProfile.ExecuteAsync(command, cancellationToken);

            return Results.Created($"/api/profiles/{result.ProfileId}", new { id = result.ProfileId });
        }
        catch (InvalidOperationException ex) when (ex.Message == CreateProfile.MaxProfilesError)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleUpdateProfile(
        Guid id,
        ClaimsPrincipal user,
        UpdateProfileRequest request,
        [FromServices] UpdateProfile updateProfile,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        try
        {
            var command = new UpdateProfile.Command(userId.Value, id, request.Name, request.Icon);
            await updateProfile.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == UpdateProfile.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleDeleteProfile(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] DeleteProfile deleteProfile,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var command = new DeleteProfile.Command(userId.Value, id);
            await deleteProfile.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == DeleteProfile.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message == DeleteProfile.CannotDeleteDefaultError)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == DeleteProfile.HasDataError)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleSetDefault(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] SetDefaultProfile setDefault,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var command = new SetDefaultProfile.Command(userId.Value, id);
            await setDefault.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == SetDefaultProfile.NotFoundError)
        {
            return Results.NotFound();
        }
    }
}

public record CreateProfileRequest(string Name, string? Icon);
public record UpdateProfileRequest(string Name, string? Icon);
