using PraxisNote.Application.Common;

namespace PraxisNote.Application.Tests.Common;

public class TiptapTextExtractorTests
{
    #region Null and Empty Input

    [Fact]
    public void Extract_NullContent_ReturnsEmpty()
    {
        var result = TiptapTextExtractor.Extract(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Extract_EmptyString_ReturnsEmpty()
    {
        var result = TiptapTextExtractor.Extract("");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Extract_WhitespaceOnly_ReturnsEmpty()
    {
        var result = TiptapTextExtractor.Extract("   ");

        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Plain Text Fallback

    [Fact]
    public void Extract_PlainText_ReturnsTrimmedText()
    {
        var result = TiptapTextExtractor.Extract("Hello world");

        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void Extract_InvalidJson_ReturnsFallbackText()
    {
        var result = TiptapTextExtractor.Extract("not json { invalid }");

        Assert.Equal("not json { invalid }", result);
    }

    #endregion

    #region Paragraph Extraction

    [Fact]
    public void Extract_SingleParagraph_ReturnsText()
    {
        var json = """
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                { "type": "text", "text": "Hello world" }
              ]
            }
          ]
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void Extract_MultipleParagraphs_ReturnsAllText()
    {
        var json = """
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [{ "type": "text", "text": "First paragraph" }]
            },
            {
              "type": "paragraph",
              "content": [{ "type": "text", "text": "Second paragraph" }]
            }
          ]
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Contains("First paragraph", result);
        Assert.Contains("Second paragraph", result);
    }

    #endregion

    #region Heading Extraction

    [Fact]
    public void Extract_Heading_ReturnsHeadingText()
    {
        var json = """
        {
          "type": "doc",
          "content": [
            {
              "type": "heading",
              "attrs": { "level": 1 },
              "content": [{ "type": "text", "text": "My Title" }]
            }
          ]
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Equal("My Title", result);
    }

    #endregion

    #region List Extraction

    [Fact]
    public void Extract_BulletList_ReturnsListItemText()
    {
        var json = """
        {
          "type": "doc",
          "content": [
            {
              "type": "bulletList",
              "content": [
                {
                  "type": "listItem",
                  "content": [
                    {
                      "type": "paragraph",
                      "content": [{ "type": "text", "text": "Item one" }]
                    }
                  ]
                },
                {
                  "type": "listItem",
                  "content": [
                    {
                      "type": "paragraph",
                      "content": [{ "type": "text", "text": "Item two" }]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Contains("Item one", result);
        Assert.Contains("Item two", result);
    }

    #endregion

    #region Code Block Extraction

    [Fact]
    public void Extract_CodeBlock_ReturnsCodeText()
    {
        var json = """
        {
          "type": "doc",
          "content": [
            {
              "type": "codeBlock",
              "content": [{ "type": "text", "text": "const x = 42;" }]
            }
          ]
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Contains("const x = 42;", result);
    }

    #endregion

    #region Mixed Content

    [Fact]
    public void Extract_MixedContent_ReturnsAllText()
    {
        var json = """
        {
          "type": "doc",
          "content": [
            {
              "type": "heading",
              "attrs": { "level": 1 },
              "content": [{ "type": "text", "text": "Meeting Notes" }]
            },
            {
              "type": "paragraph",
              "content": [{ "type": "text", "text": "We discussed the roadmap." }]
            },
            {
              "type": "bulletList",
              "content": [
                {
                  "type": "listItem",
                  "content": [
                    {
                      "type": "paragraph",
                      "content": [{ "type": "text", "text": "Action item 1" }]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Contains("Meeting Notes", result);
        Assert.Contains("We discussed the roadmap.", result);
        Assert.Contains("Action item 1", result);
    }

    #endregion

    #region Empty Document

    [Fact]
    public void Extract_EmptyDoc_ReturnsEmpty()
    {
        var json = """
        {
          "type": "doc",
          "content": []
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Extract_EmptyParagraph_ReturnsEmpty()
    {
        var json = """
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph"
            }
          ]
        }
        """;

        var result = TiptapTextExtractor.Extract(json);

        Assert.Equal(string.Empty, result);
    }

    #endregion
}
