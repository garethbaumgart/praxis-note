namespace PraxisNote.Application.Features.Notes;

public record NoteDto(
    Guid Id,
    string Content,
    IReadOnlyList<CheckboxDto> Checkboxes,
    IReadOnlyList<NoteTagDto> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CheckboxDto(
    string Id,
    string Text,
    bool IsChecked);

public record NoteTagDto(Guid Id, string Name);
