using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PraxisNote.Application.Features.Notes;
using PraxisNote.Web.Mcp;

namespace PraxisNote.Web.Mcp.Tools;

[McpServerToolType]
public sealed class NoteTools(McpUserContext userContext)
{
    [McpServerTool, Description("List all notes for the current user. Returns notes with their content, checkboxes, and tags.")]
    public async Task<string> ListNotes(GetUserNotes getUserNotes)
    {
        var query = new GetUserNotes.Query(userContext.UserId, userContext.ProfileId);
        var notes = await getUserNotes.ExecuteAsync(query);
        return JsonSerializer.Serialize(notes);
    }

    [McpServerTool, Description("Get a single note by ID with full content.")]
    public async Task<string> GetNote(
        GetNoteById getNoteById,
        [Description("The ID of the note")] string noteId)
    {
        var query = new GetNoteById.Query(Guid.Parse(noteId), userContext.UserId);
        var note = await getNoteById.ExecuteAsync(query);
        return note is null
            ? JsonSerializer.Serialize(new { error = "Note not found" })
            : JsonSerializer.Serialize(note);
    }

    [McpServerTool, Description("Create a new note. Content is optional TipTap JSON.")]
    public async Task<string> CreateNote(
        CreateNote createNote,
        [Description("Optional note content in TipTap JSON format")] string? content = null)
    {
        var command = new CreateNote.Command(userContext.UserId, userContext.ProfileId, content);
        var result = await createNote.ExecuteAsync(command);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool, Description("Update the content of an existing note.")]
    public async Task<string> UpdateNote(
        UpdateNoteContent updateNote,
        [Description("The ID of the note to update")] string noteId,
        [Description("New content in TipTap JSON format")] string content)
    {
        var command = new UpdateNoteContent.Command(Guid.Parse(noteId), userContext.UserId, content);
        var success = await updateNote.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Delete a note permanently.")]
    public async Task<string> DeleteNote(
        DeleteNote deleteNote,
        [Description("The ID of the note to delete")] string noteId)
    {
        var command = new DeleteNote.Command(Guid.Parse(noteId), userContext.UserId);
        var success = await deleteNote.ExecuteAsync(command);
        return JsonSerializer.Serialize(new { success });
    }

    [McpServerTool, Description("Add or remove a tag from a note.")]
    public async Task<string> ManageNoteTag(
        AddTagToNote addTag,
        RemoveTagFromNote removeTag,
        [Description("The ID of the note")] string noteId,
        [Description("The ID of the tag")] string tagId,
        [Description("Action: 'add' or 'remove'")] string action)
    {
        if (!Guid.TryParse(noteId, out var parsedNoteId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid note ID format" });
        if (!Guid.TryParse(tagId, out var parsedTagId))
            return JsonSerializer.Serialize(new { success = false, error = "Invalid tag ID format" });

        if (action.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            await addTag.ExecuteAsync(new AddTagToNote.Command(userContext.UserId, parsedNoteId, parsedTagId));
            return JsonSerializer.Serialize(new { success = true, action = "added" });
        }

        if (action.Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            await removeTag.ExecuteAsync(new RemoveTagFromNote.Command(userContext.UserId, parsedNoteId, parsedTagId));
            return JsonSerializer.Serialize(new { success = true, action = "removed" });
        }

        return JsonSerializer.Serialize(new { success = false, error = $"Invalid action '{action}'. Use 'add' or 'remove'." });
    }
}
