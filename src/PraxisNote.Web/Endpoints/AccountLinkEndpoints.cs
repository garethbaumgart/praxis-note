using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PraxisNote.Application.Features.AccountLinking;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class AccountLinkEndpoints
{
    private const string AvatarUrlClaim = "avatar_url";
    private const string ProviderClaim = "provider";

    public static void MapAccountLinkEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/account")
            .RequireAuthorization();

        group.MapPost("/link-code", (Delegate)HandleGenerateLinkCode);
        group.MapPost("/link", (Delegate)HandleRedeemLinkCode);
        group.MapGet("/linked-identities", (Delegate)HandleGetLinkedIdentities);
        group.MapDelete("/linked-identities/{id:guid}", (Delegate)HandleUnlinkIdentity);
        group.MapPut("/linked-identities/{id:guid}/default-profile", (Delegate)HandleSetDefaultProfile);
    }

    private static async Task<IResult> HandleGenerateLinkCode(
        ClaimsPrincipal user,
        [FromServices] GenerateLinkCode generateLinkCode,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var command = new GenerateLinkCode.Command(userId.Value);
        var result = await generateLinkCode.ExecuteAsync(command, cancellationToken);

        return Results.Ok(new { code = result.Code, expiresAt = result.ExpiresAt });
    }

    private static async Task<IResult> HandleRedeemLinkCode(
        HttpContext context,
        ClaimsPrincipal user,
        RedeemLinkCodeRequest request,
        [FromServices] RedeemLinkCode redeemLinkCode,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(new { error = "Code is required" });
        }

        if (!Enum.TryParse<MergeStrategy>(request.MergeStrategy, true, out var strategy))
        {
            return Results.BadRequest(new { error = "Invalid merge strategy. Use: MergeIntoExisting, CreateNewProfile, or Cancel" });
        }

        var command = new RedeemLinkCode.Command(
            userId.Value,
            request.Code,
            strategy,
            request.TargetProfileId);

        var result = await redeemLinkCode.ExecuteAsync(command, cancellationToken);

        if (!result.Success)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        // Re-sign the session if the user was merged into a different account
        if (result.TargetUserId != userId.Value)
        {
            var email = user.FindFirstValue(ClaimTypes.Email) ?? "";
            var name = user.FindFirstValue(ClaimTypes.Name) ?? "";
            var avatarUrl = user.FindFirstValue(AvatarUrlClaim);
            var provider = user.FindFirstValue(ProviderClaim) ?? "";

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, result.TargetUserId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(ProviderClaim, provider)
            ], CookieAuthenticationDefaults.AuthenticationScheme);

            if (!string.IsNullOrEmpty(avatarUrl))
            {
                identity.AddClaim(new Claim(AvatarUrlClaim, avatarUrl));
            }

            var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });
        }

        return Results.Ok(new { targetUserId = result.TargetUserId });
    }

    private static async Task<IResult> HandleGetLinkedIdentities(
        ClaimsPrincipal user,
        [FromServices] GetLinkedIdentities getLinkedIdentities,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        var query = new GetLinkedIdentities.Query(userId.Value);
        var identities = await getLinkedIdentities.ExecuteAsync(query, cancellationToken);

        return Results.Ok(identities);
    }

    private static async Task<IResult> HandleUnlinkIdentity(
        Guid id,
        ClaimsPrincipal user,
        [FromServices] UnlinkIdentity unlinkIdentity,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var command = new UnlinkIdentity.Command(userId.Value, id);
            await unlinkIdentity.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == UnlinkIdentity.NotFoundError)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message == UnlinkIdentity.LastIdentityError)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> HandleSetDefaultProfile(
        Guid id,
        ClaimsPrincipal user,
        SetDefaultProfileRequest request,
        [FromServices] SetIdentityDefaultProfile setDefault,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (userId is null) return Results.Unauthorized();

        try
        {
            var command = new SetIdentityDefaultProfile.Command(userId.Value, id, request.ProfileId);
            await setDefault.ExecuteAsync(command, cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == SetIdentityDefaultProfile.IdentityNotFoundError)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message == SetIdentityDefaultProfile.ProfileNotFoundError)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public record RedeemLinkCodeRequest(string Code, string MergeStrategy, Guid? TargetProfileId = null);
public record SetDefaultProfileRequest(Guid? ProfileId);
