using PraxisNote.Domain.Aggregates.Notifications;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.Aggregates;

public class FeatureNotificationTests
{
    private const string ValidTitle = "New Feature";
    private const string ValidSummary = "This is a summary of the new feature.";
    private const string ValidIssueUrl = "https://github.com/example/repo/issues/123";

    #region Create Tests - Happy Path

    [Theory]
    [InlineData(NotificationType.Feature)]
    [InlineData(NotificationType.BugFix)]
    [InlineData(NotificationType.Improvement)]
    public void Create_WithValidParameters_ReturnsNotification(NotificationType type)
    {
        // Act
        var notification = FeatureNotification.Create(type, ValidTitle, ValidSummary, ValidIssueUrl);

        // Assert
        Assert.Equal(type, notification.Type);
        Assert.Equal(ValidTitle, notification.Title);
        Assert.Equal(ValidSummary, notification.Summary);
        Assert.Equal(ValidIssueUrl, notification.IssueUrl);
        Assert.True(notification.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithoutIssueUrl_SetsIssueUrlToNull()
    {
        // Act
        var notification = FeatureNotification.Create(NotificationType.Feature, ValidTitle, ValidSummary);

        // Assert
        Assert.Null(notification.IssueUrl);
    }

    #endregion

    #region Create Tests - Title Validation

    [Fact]
    public void Create_WithNullTitle_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FeatureNotification.Create(NotificationType.Feature, null!, ValidSummary));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WithEmptyOrWhitespaceTitle_ThrowsArgumentException(string invalidTitle)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            FeatureNotification.Create(NotificationType.Feature, invalidTitle, ValidSummary));
    }

    [Fact]
    public void Create_TrimsTitleWhitespace()
    {
        // Act
        var notification = FeatureNotification.Create(
            NotificationType.Feature,
            "  Trimmed Title  ",
            ValidSummary);

        // Assert
        Assert.Equal("Trimmed Title", notification.Title);
    }

    #endregion

    #region Create Tests - Summary Validation

    [Fact]
    public void Create_WithNullSummary_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FeatureNotification.Create(NotificationType.Feature, ValidTitle, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_WithEmptyOrWhitespaceSummary_ThrowsArgumentException(string invalidSummary)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            FeatureNotification.Create(NotificationType.Feature, ValidTitle, invalidSummary));
    }

    [Fact]
    public void Create_TrimsSummaryWhitespace()
    {
        // Act
        var notification = FeatureNotification.Create(
            NotificationType.Feature,
            ValidTitle,
            "  Trimmed Summary  ");

        // Assert
        Assert.Equal("Trimmed Summary", notification.Summary);
    }

    #endregion

    #region Create Tests - IssueUrl Handling

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceIssueUrl_SetsIssueUrlToNull(string? issueUrl)
    {
        // Act
        var notification = FeatureNotification.Create(
            NotificationType.Feature,
            ValidTitle,
            ValidSummary,
            issueUrl);

        // Assert
        Assert.Null(notification.IssueUrl);
    }

    [Fact]
    public void Create_TrimsIssueUrlWhitespace()
    {
        // Act
        var notification = FeatureNotification.Create(
            NotificationType.Feature,
            ValidTitle,
            ValidSummary,
            "  https://example.com/issue  ");

        // Assert
        Assert.Equal("https://example.com/issue", notification.IssueUrl);
    }

    #endregion

    #region Create Tests - Timestamp

    [Fact]
    public void Create_SetsCreatedAtToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var notification = FeatureNotification.Create(
            NotificationType.Feature,
            ValidTitle,
            ValidSummary);

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.True(notification.CreatedAt >= before);
        Assert.True(notification.CreatedAt <= after);
    }

    #endregion
}
