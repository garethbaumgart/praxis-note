using PraxisNote.Application.Features.Notes.Services;

namespace PraxisNote.Application.Tests.Notes;

public class CheckboxUpdaterTests
{
    private readonly TiptapCheckboxUpdater _updater = new();

    #region Invalid Input

    [Fact]
    public void UpdateCheckboxState_WithNullContent_ReturnsNull()
    {
        var result = _updater.UpdateCheckboxState(null!, "cb-1", true);

        Assert.Null(result);
    }

    [Fact]
    public void UpdateCheckboxState_WithEmptyContent_ReturnsNull()
    {
        var result = _updater.UpdateCheckboxState("", "cb-1", true);

        Assert.Null(result);
    }

    [Fact]
    public void UpdateCheckboxState_WithInvalidJson_ReturnsNull()
    {
        var result = _updater.UpdateCheckboxState("not valid json", "cb-1", true);

        Assert.Null(result);
    }

    [Fact]
    public void UpdateCheckboxState_WithInvalidCheckboxId_ReturnsNull()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Test" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _updater.UpdateCheckboxState(json, "invalid-id", true);

        Assert.Null(result);
    }

    [Fact]
    public void UpdateCheckboxState_WithNonExistentCheckbox_ReturnsNull()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Test" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        // Only 1 checkbox exists, asking for cb-5
        var result = _updater.UpdateCheckboxState(json, "cb-5", true);

        Assert.Null(result);
    }

    #endregion

    #region Single Checkbox

    [Fact]
    public void UpdateCheckboxState_CheckFirstCheckbox_UpdatesToChecked()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Test" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _updater.UpdateCheckboxState(json, "cb-1", true);

        Assert.NotNull(result);
        Assert.Contains("\"checked\":true", result);
    }

    [Fact]
    public void UpdateCheckboxState_UncheckFirstCheckbox_UpdatesToUnchecked()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": true },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Test" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _updater.UpdateCheckboxState(json, "cb-1", false);

        Assert.NotNull(result);
        Assert.Contains("\"checked\":false", result);
    }

    #endregion

    #region Multiple Checkboxes

    [Fact]
    public void UpdateCheckboxState_UpdateSecondCheckbox_OnlyUpdatesSecond()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "First" }] }]
                        },
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Second" }] }]
                        },
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Third" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _updater.UpdateCheckboxState(json, "cb-2", true);

        Assert.NotNull(result);
        // The result should have exactly one "checked":true (the second one)
        // and two "checked":false
        var checkedTrueCount = result.Split("\"checked\":true").Length - 1;
        var checkedFalseCount = result.Split("\"checked\":false").Length - 1;
        Assert.Equal(1, checkedTrueCount);
        Assert.Equal(2, checkedFalseCount);
    }

    [Fact]
    public void UpdateCheckboxState_UpdateThirdCheckbox_UpdatesThird()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": true },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "First" }] }]
                        },
                        {
                            "type": "taskItem",
                            "attrs": { "checked": true },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Second" }] }]
                        },
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "Third" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _updater.UpdateCheckboxState(json, "cb-3", true);

        Assert.NotNull(result);
        // All three should now be checked
        var checkedTrueCount = result.Split("\"checked\":true").Length - 1;
        Assert.Equal(3, checkedTrueCount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UpdateCheckboxState_WithMissingAttrs_CreatesAttrsAndSetsChecked()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "No attrs" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _updater.UpdateCheckboxState(json, "cb-1", true);

        Assert.NotNull(result);
        Assert.Contains("\"checked\":true", result);
    }

    [Fact]
    public void UpdateCheckboxState_WithMultipleTaskLists_CountsAcrossLists()
    {
        var json = """
        {
            "type": "doc",
            "content": [
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "List 1 Item 1" }] }]
                        }
                    ]
                },
                {
                    "type": "paragraph",
                    "content": [{ "type": "text", "text": "Some text" }]
                },
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [{ "type": "paragraph", "content": [{ "type": "text", "text": "List 2 Item 1" }] }]
                        }
                    ]
                }
            ]
        }
        """;

        // Update cb-2 which is in the second taskList
        var result = _updater.UpdateCheckboxState(json, "cb-2", true);

        Assert.NotNull(result);
        // First checkbox should still be false, second should be true
        var checkedTrueCount = result.Split("\"checked\":true").Length - 1;
        var checkedFalseCount = result.Split("\"checked\":false").Length - 1;
        Assert.Equal(1, checkedTrueCount);
        Assert.Equal(1, checkedFalseCount);
    }

    #endregion
}
