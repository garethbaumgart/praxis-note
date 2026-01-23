using System.Text.Json;
using System.Text.Json.Nodes;

namespace PraxisNote.Application.Features.Notes.Services;

/// <summary>
/// Service for updating checkbox state in TipTap JSON content.
/// </summary>
public interface ICheckboxUpdater
{
    /// <summary>
    /// Updates a checkbox's checked state in the TipTap JSON content.
    /// </summary>
    /// <param name="content">The TipTap JSON content string.</param>
    /// <param name="checkboxId">The checkbox ID (e.g., "cb-1").</param>
    /// <param name="isChecked">The new checked state.</param>
    /// <returns>The updated content string, or null if the checkbox wasn't found.</returns>
    string? UpdateCheckboxState(string content, string checkboxId, bool isChecked);
}

/// <summary>
/// Implementation of <see cref="ICheckboxUpdater"/> for TipTap JSON format.
/// </summary>
public sealed class TiptapCheckboxUpdater : ICheckboxUpdater
{
    public string? UpdateCheckboxState(string content, string checkboxId, bool isChecked)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var doc = JsonNode.Parse(content);
            if (doc is null)
                return null;

            // Extract the checkbox number from the ID (e.g., "cb-1" -> 1)
            if (!checkboxId.StartsWith("cb-") ||
                !int.TryParse(checkboxId[3..], out var targetNumber))
            {
                return null;
            }

            var counter = 0;
            var found = UpdateTaskItemsRecursively(doc, targetNumber, isChecked, ref counter);

            if (!found)
                return null;

            return doc.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private bool UpdateTaskItemsRecursively(JsonNode node, int targetNumber, bool isChecked, ref int counter)
    {
        if (node is JsonObject obj)
        {
            // Check if this is a taskItem
            if (obj.TryGetPropertyValue("type", out var typeNode) &&
                typeNode?.GetValue<string>() == "taskItem")
            {
                // Count this task item
                counter++;

                if (counter == targetNumber)
                {
                    // Found our target - update the checked state
                    if (!obj.ContainsKey("attrs"))
                    {
                        obj["attrs"] = new JsonObject();
                    }

                    var attrs = obj["attrs"]!.AsObject();
                    attrs["checked"] = isChecked;
                    return true;
                }
            }

            // Recursively check content
            if (obj.TryGetPropertyValue("content", out var contentNode) &&
                contentNode is JsonArray contentArray)
            {
                foreach (var child in contentArray)
                {
                    if (child is not null && UpdateTaskItemsRecursively(child, targetNumber, isChecked, ref counter))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
