using System.Text.Json;

namespace PraxisNote.Application.Common;

/// <summary>
/// Extracts a display title from TipTap JSON or plain text note content.
/// Shared across features that need to display note titles in summaries.
/// </summary>
public static class NoteTitleExtractor
{
    /// <summary>
    /// Extracts a display title from TipTap JSON content.
    /// Returns the first heading text, or the first paragraph text, or "Untitled Note".
    /// </summary>
    public static string Extract(string content, int maxLength = 60)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Untitled Note";

        try
        {
            using var doc = JsonDocument.Parse(content);
            var title = FindFirstTitle(doc.RootElement);
            if (!string.IsNullOrWhiteSpace(title))
                return title.Length > maxLength ? title[..(maxLength - 3)] + "..." : title;
        }
        catch (JsonException)
        {
            // Content is not valid JSON — try plain text fallback
            var firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(firstLine))
                return firstLine.Length > maxLength ? firstLine[..(maxLength - 3)] + "..." : firstLine;
        }

        return "Untitled Note";
    }

    /// <summary>
    /// Recursively searches a TipTap JSON tree for the first heading or paragraph with text.
    /// Handles nested structures like taskList, bulletList, and listItem.
    /// </summary>
    private static string? FindFirstTitle(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return null;

        if (node.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();
            if (type is "heading" or "paragraph")
            {
                var text = ExtractTextFromNode(node);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        if (node.TryGetProperty("content", out var contentArray) &&
            contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in contentArray.EnumerateArray())
            {
                var found = FindFirstTitle(child);
                if (!string.IsNullOrWhiteSpace(found))
                    return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts text content from a TipTap node's immediate text children.
    /// </summary>
    private static string ExtractTextFromNode(JsonElement node)
    {
        if (!node.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var parts = content.EnumerateArray()
            .Where(child =>
                child.TryGetProperty("type", out var childType) &&
                childType.GetString() == "text" &&
                child.TryGetProperty("text", out _))
            .Select(child => child.GetProperty("text").GetString() ?? string.Empty);

        return string.Join("", parts);
    }
}
