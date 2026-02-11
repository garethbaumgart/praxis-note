namespace PraxisNote.Application.Features.AccountLinking;

public record LinkedIdentityDto(
    Guid Id,
    string Provider,
    string Email,
    string Name,
    string? AvatarUrl,
    Guid? DefaultProfileId,
    DateTimeOffset LinkedAt);
