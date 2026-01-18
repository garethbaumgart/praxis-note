using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Domain.Tests.Aggregates;

public class CommentTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidContent_ReturnsComment()
    {
        // Arrange
        var content = "This is a valid comment";

        // Act
        var comment = Comment.Create(content);

        // Assert
        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(content, comment.Content);
        Assert.True(comment.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(comment.CreatedAt, comment.UpdatedAt);
    }

    [Fact]
    public void Create_WithNullContent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Comment.Create(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WithEmptyOrWhitespaceContent_ThrowsArgumentException(string invalidContent)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Comment.Create(invalidContent));
    }

    [Fact]
    public void Create_TrimsContentWhitespace()
    {
        // Arrange
        var contentWithWhitespace = "  trimmed content  ";

        // Act
        var comment = Comment.Create(contentWithWhitespace);

        // Assert
        Assert.Equal("trimmed content", comment.Content);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        // Act
        var comment1 = Comment.Create("Comment 1");
        var comment2 = Comment.Create("Comment 2");

        // Assert
        Assert.NotEqual(comment1.Id, comment2.Id);
    }

    #endregion

    #region WithUpdatedContent Tests

    [Fact]
    public void WithUpdatedContent_WithValidContent_ReturnsNewComment()
    {
        // Arrange
        var original = Comment.Create("Original content");

        // Act
        var updated = original.WithUpdatedContent("Updated content");

        // Assert
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal("Updated content", updated.Content);
        Assert.Equal(original.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt >= original.UpdatedAt);
    }

    [Fact]
    public void WithUpdatedContent_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange
        var comment = Comment.Create("Original content");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => comment.WithUpdatedContent(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void WithUpdatedContent_WithEmptyOrWhitespaceContent_ThrowsArgumentException(string invalidContent)
    {
        // Arrange
        var comment = Comment.Create("Original content");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => comment.WithUpdatedContent(invalidContent));
    }

    [Fact]
    public void WithUpdatedContent_TrimsContentWhitespace()
    {
        // Arrange
        var comment = Comment.Create("Original content");

        // Act
        var updated = comment.WithUpdatedContent("  trimmed update  ");

        // Assert
        Assert.Equal("trimmed update", updated.Content);
    }

    [Fact]
    public void WithUpdatedContent_DoesNotModifyOriginalComment()
    {
        // Arrange
        var original = Comment.Create("Original content");
        var originalContent = original.Content;
        var originalUpdatedAt = original.UpdatedAt;

        // Act
        original.WithUpdatedContent("New content");

        // Assert - Original unchanged (immutability)
        Assert.Equal(originalContent, original.Content);
        Assert.Equal(originalUpdatedAt, original.UpdatedAt);
    }

    #endregion
}
