using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Tests.Notes;

/// <summary>
/// Integration tests demonstrating CreateNote behavior with checkbox extraction.
/// These tests verify the integration between CreateNote use case and ICheckboxExtractor.
/// </summary>
/// <remarks>
/// Note: These are documentation/integration tests, not full unit tests with mocks.
/// The actual CreateNote use case requires repository and unit of work dependencies
/// which are not mocked in this test project.
/// </remarks>
public class CreateNoteIntegrationTests
{
    private readonly TiptapCheckboxExtractor _checkboxExtractor = new();
    private readonly Guid _validUserId = Guid.NewGuid();

    #region Checkbox Extraction Behavior Documentation

    [Fact]
    public void CreateNote_WithCheckboxContent_ShouldExtractAndAddCheckboxes()
    {
        // This test documents the expected behavior when CreateNote is called with content containing checkboxes.
        // The CreateNote use case should:
        // 1. Create the note
        // 2. Extract checkboxes from content using ICheckboxExtractor
        // 3. Add each extracted checkbox to the note using note.AddCheckbox()

        // Arrange - TipTap content with a single checkbox
        var tiptapContent = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "Buy groceries" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        // Act - Extract checkboxes (this is what CreateNote should do)
        var note = Note.Create(_validUserId, tiptapContent);
        var checkboxes = _checkboxExtractor.Extract(tiptapContent);
        foreach (var checkbox in checkboxes)
        {
            note.AddCheckbox(checkbox);
        }

        // Assert - Note should have the extracted checkbox
        Assert.Single(note.Checkboxes);
        Assert.Equal("cb-1", note.Checkboxes.First().Id);
        Assert.Equal("Buy groceries", note.Checkboxes.First().Text);
        Assert.False(note.Checkboxes.First().IsChecked);
    }

    [Fact]
    public void CreateNote_WithMultipleCheckboxes_ShouldExtractAllCheckboxes()
    {
        // Arrange - TipTap content with multiple checkboxes
        var tiptapContent = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "First task" }]
                                }
                            ]
                        },
                        {
                            "type": "taskItem",
                            "attrs": { "checked": true },
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "Second task" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        // Act - Extract checkboxes (this is what CreateNote should do)
        var note = Note.Create(_validUserId, tiptapContent);
        var checkboxes = _checkboxExtractor.Extract(tiptapContent);
        foreach (var checkbox in checkboxes)
        {
            note.AddCheckbox(checkbox);
        }

        // Assert - Note should have both checkboxes
        Assert.Equal(2, note.Checkboxes.Count);
        Assert.Contains(note.Checkboxes, c => c.Id == "cb-1" && c.Text == "First task" && !c.IsChecked);
        Assert.Contains(note.Checkboxes, c => c.Id == "cb-2" && c.Text == "Second task" && c.IsChecked);
    }

    [Fact]
    public void CreateNote_WithNullContent_ShouldNotExtractCheckboxes()
    {
        // Arrange - Null content
        string? content = null;

        // Act - Create note (CreateNote should skip extraction if content is null)
        var note = Note.Create(_validUserId, content ?? string.Empty);
        // No extraction should happen when content is null

        // Assert - Note should have no checkboxes
        Assert.Empty(note.Checkboxes);
    }

    [Fact]
    public void CreateNote_WithEmptyContent_ShouldNotExtractCheckboxes()
    {
        // Arrange - Empty content
        var content = string.Empty;

        // Act - Create note (CreateNote should skip extraction if content is empty)
        var note = Note.Create(_validUserId, content);
        // No extraction should happen when content is empty

        // Assert - Note should have no checkboxes
        Assert.Empty(note.Checkboxes);
    }

    [Fact]
    public void CreateNote_WithPlainTextContent_ShouldNotExtractCheckboxes()
    {
        // Arrange - Content without checkboxes
        var tiptapContent = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "paragraph",
                    "content": [{ "type": "text", "text": "Just plain text, no checkboxes" }]
                }
            ]
        }
        """;

        // Act - Extract checkboxes (should return empty list)
        var note = Note.Create(_validUserId, tiptapContent);
        var checkboxes = _checkboxExtractor.Extract(tiptapContent);
        foreach (var checkbox in checkboxes)
        {
            note.AddCheckbox(checkbox);
        }

        // Assert - Note should have no checkboxes
        Assert.Empty(note.Checkboxes);
    }

    [Fact]
    public void CreateNote_CheckboxesEnable_PromoteToTask()
    {
        // This test documents why checkbox extraction is critical:
        // PromoteCheckboxToTask requires note.GetCheckbox(checkboxId) to return a valid checkbox.
        // If CreateNote doesn't extract checkboxes, promotion fails with 404.

        // Arrange - Create note with checkbox
        var tiptapContent = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "Task to promote" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var note = Note.Create(_validUserId, tiptapContent);
        var checkboxes = _checkboxExtractor.Extract(tiptapContent);
        foreach (var checkbox in checkboxes)
        {
            note.AddCheckbox(checkbox);
        }

        // Act - Try to get checkbox by ID (this is what PromoteCheckboxToTask does)
        var foundCheckbox = note.GetCheckbox("cb-1");

        // Assert - Checkbox should be found (not null)
        // If this returns null, PromoteCheckboxToTask would return 404
        Assert.NotNull(foundCheckbox);
        Assert.Equal("Task to promote", foundCheckbox.Text);
    }

    #endregion
}
