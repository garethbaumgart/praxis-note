using System.Text.Json;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Features.Notes.Services;

/// <summary>
/// Service for extracting checkboxes from TipTap JSON content.
/// </summary>
public interface ICheckboxExtractor
{
    /// <summary>
    /// Extracts all task items (checkboxes) from TipTap JSON content.
    /// </summary>
    /// <param name="content">The TipTap JSON content string.</param>
    /// <returns>A list of extracted checkboxes with IDs, text, and checked state.</returns>
    IReadOnlyList<Checkbox> Extract(string content);
}

/// <summary>
/// Implementation of <see cref="ICheckboxExtractor"/> for TipTap JSON format.
/// </summary>
/// <remarks>
/// TipTap stores task lists in this structure:
/// {
///   "type": "doc",
///   "content": [
///     {
///       "type": "taskList",
///       "content": [
///         {
///           "type": "taskItem",
///           "attrs": { "checked": false },
///           "content": [
///             {
///               "type": "paragraph",
///               "content": [{ "type": "text", "text": "Task text" }]
///             }
///           ]
///         }
///       ]
///     }
///   ]
/// }
/// </remarks>
public sealed class TiptapCheckboxExtractor : ICheckboxExtractor
{
    private int _idCounter;

    public IReadOnlyList<Checkbox> Extract(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        _idCounter = 0;
        var checkboxes = new List<Checkbox>();

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("content", out var contentArray))
            {
                ExtractFromNodes(contentArray, checkboxes);
            }
        }
        catch (JsonException)
        {
            // Not valid JSON, return empty list
            return [];
        }

        return checkboxes;
    }

    private void ExtractFromNodes(JsonElement nodes, List<Checkbox> checkboxes)
    {
        if (nodes.ValueKind != JsonValueKind.Array)
            return;

        foreach (var node in nodes.EnumerateArray())
        {
            if (!node.TryGetProperty("type", out var typeElement))
                continue;

            var nodeType = typeElement.GetString();

            if (nodeType == "taskItem")
            {
                var checkbox = ExtractTaskItem(node);
                if (checkbox != null)
                {
                    checkboxes.Add(checkbox);
                }
            }
            else if (node.TryGetProperty("content", out var childContent))
            {
                // Recursively search in nested content (e.g., taskList, bulletList, etc.)
                ExtractFromNodes(childContent, checkboxes);
            }
        }
    }

    private Checkbox? ExtractTaskItem(JsonElement taskItem)
    {
        // Get checked state from attrs
        var isChecked = false;
        if (taskItem.TryGetProperty("attrs", out var attrs) &&
            attrs.TryGetProperty("checked", out var checkedProp))
        {
            isChecked = checkedProp.ValueKind == JsonValueKind.True;
        }

        // Extract text from nested content
        var text = ExtractTextFromNode(taskItem);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Generate a unique ID for this checkbox
        // TipTap doesn't provide IDs by default, so we generate sequential IDs
        // These IDs are stable as long as the checkbox order doesn't change
        _idCounter++;
        var id = $"cb-{_idCounter}";

        return new Checkbox(id, text, isChecked);
    }

    private string ExtractTextFromNode(JsonElement node)
    {
        if (!node.TryGetProperty("content", out var content))
            return string.Empty;

        var textParts = new List<string>();

        foreach (var child in content.EnumerateArray())
        {
            if (!child.TryGetProperty("type", out var typeElement))
                continue;

            var childType = typeElement.GetString();

            if (childType == "text")
            {
                if (child.TryGetProperty("text", out var textProp))
                {
                    textParts.Add(textProp.GetString() ?? string.Empty);
                }
            }
            else if (child.TryGetProperty("content", out _))
            {
                // Recursively extract text from nested nodes (e.g., paragraph)
                textParts.Add(ExtractTextFromNode(child));
            }
        }

        return string.Join("", textParts).Trim();
    }
}
