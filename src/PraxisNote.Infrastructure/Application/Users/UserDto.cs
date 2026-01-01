namespace PraxisNote.Infrastructure.Application.Users;

public record UserDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string Provider);
