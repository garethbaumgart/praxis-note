namespace PraxisNote.Application.Features.Tags;

public record TagDto(Guid Id, string Name, string Color, int UsageCount);

public record TaskTagDto(Guid Id, string Name, string Color);
