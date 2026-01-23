using PraxisNote.Application.Features.Notes.Services;

namespace PraxisNote.Application.Tests.Notes;

public class CheckboxExtractorTests
{
    private readonly TiptapCheckboxExtractor _extractor = new();

    #region Empty/Invalid Content

    [Fact]
    public void Extract_WithNullContent_ReturnsEmptyList()
    {
        var result = _extractor.Extract(null!);

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_WithEmptyString_ReturnsEmptyList()
    {
        var result = _extractor.Extract("");

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_WithWhitespace_ReturnsEmptyList()
    {
        var result = _extractor.Extract("   ");

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_WithInvalidJson_ReturnsEmptyList()
    {
        var result = _extractor.Extract("not valid json");

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_WithJsonWithoutContent_ReturnsEmptyList()
    {
        var result = _extractor.Extract("""{"type":"doc"}""");

        Assert.Empty(result);
    }

    #endregion

    #region Single Checkbox

    [Fact]
    public void Extract_WithSingleUncheckedCheckbox_ReturnsOne()
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

        var result = _extractor.Extract(json);

        Assert.Single(result);
        Assert.Equal("cb-1", result[0].Id);
        Assert.Equal("Buy groceries", result[0].Text);
        Assert.False(result[0].IsChecked);
    }

    [Fact]
    public void Extract_WithSingleCheckedCheckbox_ReturnsOneChecked()
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
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "Complete task" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _extractor.Extract(json);

        Assert.Single(result);
        Assert.True(result[0].IsChecked);
    }

    #endregion

    #region Multiple Checkboxes

    [Fact]
    public void Extract_WithMultipleCheckboxes_ReturnsAll()
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
                        },
                        {
                            "type": "taskItem",
                            "attrs": { "checked": false },
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "Third task" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _extractor.Extract(json);

        Assert.Equal(3, result.Count);
        Assert.Equal("cb-1", result[0].Id);
        Assert.Equal("First task", result[0].Text);
        Assert.False(result[0].IsChecked);
        Assert.Equal("cb-2", result[1].Id);
        Assert.Equal("Second task", result[1].Text);
        Assert.True(result[1].IsChecked);
        Assert.Equal("cb-3", result[2].Id);
        Assert.Equal("Third task", result[2].Text);
        Assert.False(result[2].IsChecked);
    }

    [Fact]
    public void Extract_WithMultipleTaskLists_ReturnsAllCheckboxes()
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
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "List 1 Item 1" }]
                                }
                            ]
                        }
                    ]
                },
                {
                    "type": "paragraph",
                    "content": [{ "type": "text", "text": "Some text between" }]
                },
                {
                    "type": "taskList",
                    "content": [
                        {
                            "type": "taskItem",
                            "attrs": { "checked": true },
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "List 2 Item 1" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _extractor.Extract(json);

        Assert.Equal(2, result.Count);
        Assert.Equal("List 1 Item 1", result[0].Text);
        Assert.Equal("List 2 Item 1", result[1].Text);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Extract_WithEmptyCheckboxText_SkipsCheckbox()
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
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": []
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _extractor.Extract(json);

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_WithWhitespaceOnlyText_SkipsCheckbox()
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
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "   " }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _extractor.Extract(json);

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_WithMissingCheckedAttr_DefaultsToFalse()
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
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [{ "type": "text", "text": "No attrs" }]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _extractor.Extract(json);

        Assert.Single(result);
        Assert.False(result[0].IsChecked);
    }

    [Fact]
    public void Extract_WithPlainTextContent_ReturnsEmptyList()
    {
        var json = """
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

        var result = _extractor.Extract(json);

        Assert.Empty(result);
    }

    [Fact]
    public void Extract_WithMultipleTextNodes_ConcatenatesText()
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
                            "content": [
                                {
                                    "type": "paragraph",
                                    "content": [
                                        { "type": "text", "text": "Hello " },
                                        { "type": "text", "text": "World" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        var result = _extractor.Extract(json);

        Assert.Single(result);
        Assert.Equal("Hello World", result[0].Text);
    }

    #endregion
}
