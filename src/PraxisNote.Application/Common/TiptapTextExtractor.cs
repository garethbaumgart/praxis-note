using System.Text;
using System.Text.Json;

namespace PraxisNote.Application.Common;

/// <summary>
/// Extracts all plain text from TipTap JSON content recursively.
/// Used to provide full note content as context for AI features.
/// </summary>
public static class TiptapTextExtractor
{
    /// <summary>
    /// Extracts all plain text from TipTap JSON content.
    /// Returns empty string for null/invalid content.
    /// Falls back to raw string for non-JSON content.
    /// </summary>
    public static string Extract(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var sb = new StringBuilder();
            ExtractTextRecursive(doc.RootElement, sb);
            return sb.ToString().Trim();
        }
        catch (JsonException)
        {
            // Content is not valid JSON — return raw string as fallback
            return content.Trim();
        }
    }

    private static void ExtractTextRecursive(JsonElement node, StringBuilder sb)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();

                // Extract text from text nodes
                if (type == "text" && node.TryGetProperty("text", out var textElement))
                {
                    sb.Append(textElement.GetString());
                    return;
                }

                // Add newlines after block-level elements
                if (type is "paragraph" or "heading" or "codeBlock" or "blockquote"
                    or "bulletList" or "orderedList" or "listItem" or "taskList" or "taskItem"
                    or "horizontalRule")
                {
                    if (node.TryGetProperty("content", out var blockContent) &&
                        blockContent.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var child in blockContent.EnumerateArray())
                        {
                            ExtractTextRecursive(child, sb);
                        }
                    }

                    if (type is "paragraph" or "heading" or "codeBlock" or "blockquote" or "listItem" or "taskItem")
                    {
                        sb.AppendLine();
                    }

                    return;
                }
            }

            // For any other object, recurse into content array
            if (node.TryGetProperty("content", out var contentArray) &&
                contentArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in contentArray.EnumerateArray())
                {
                    ExtractTextRecursive(child, sb);
                }
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
            {
                ExtractTextRecursive(child, sb);
            }
        }
    }
}
