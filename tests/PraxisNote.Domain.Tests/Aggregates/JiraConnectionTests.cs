using PraxisNote.Domain.Aggregates.JiraConnections;

namespace PraxisNote.Domain.Tests.Aggregates;

public class JiraConnectionTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProfileId = Guid.NewGuid();
    private const string ValidCloudId = "cloud-123";
    private const string ValidSiteUrl = "https://myorg.atlassian.net";
    private const string ValidAccessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOi";
    private const string ValidRefreshToken = "refresh-token-abc";
    private readonly DateTimeOffset _validExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_CreatesConnection()
    {
        // Act
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        // Assert
        Assert.NotEqual(Guid.Empty, connection.Id);
        Assert.Equal(_validUserId, connection.UserId);
        Assert.Equal(_validProfileId, connection.ProfileId);
        Assert.Equal(ValidCloudId, connection.CloudId);
        Assert.Equal(ValidSiteUrl, connection.SiteUrl);
        Assert.Equal(ValidAccessToken, connection.AccessToken);
        Assert.Equal(ValidRefreshToken, connection.RefreshToken);
        Assert.Equal(_validExpiresAt, connection.TokenExpiresAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            JiraConnection.Create(Guid.Empty, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt));
    }

    [Fact]
    public void Create_WithEmptyProfileId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            JiraConnection.Create(_validUserId, Guid.Empty, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCloudId_ThrowsArgumentException(string? cloudId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            JiraConnection.Create(_validUserId, _validProfileId, cloudId!, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidSiteUrl_ThrowsArgumentException(string? siteUrl)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            JiraConnection.Create(_validUserId, _validProfileId, ValidCloudId, siteUrl!, ValidAccessToken, ValidRefreshToken, _validExpiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidAccessToken_ThrowsArgumentException(string? accessToken)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            JiraConnection.Create(_validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, accessToken!, ValidRefreshToken, _validExpiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidRefreshToken_ThrowsArgumentException(string? refreshToken)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            JiraConnection.Create(_validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, refreshToken!, _validExpiresAt));
    }

    [Fact]
    public void Create_TrimsCloudIdAndSiteUrl()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, "  cloud-123  ", "  https://myorg.atlassian.net  ", ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.Equal("cloud-123", connection.CloudId);
        Assert.Equal("https://myorg.atlassian.net", connection.SiteUrl);
    }

    #endregion

    #region UpdateTokens Tests

    [Fact]
    public void UpdateTokens_WithNewAccessToken_UpdatesAccessTokenAndExpiry()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        var newExpiry = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        connection.UpdateTokens("new-access-token", newExpiry);

        // Assert
        Assert.Equal("new-access-token", connection.AccessToken);
        Assert.Equal(newExpiry, connection.TokenExpiresAt);
        Assert.Equal(ValidRefreshToken, connection.RefreshToken); // unchanged
    }

    [Fact]
    public void UpdateTokens_WithNewRefreshToken_UpdatesAllTokens()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        var newExpiry = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        connection.UpdateTokens("new-access", newExpiry, "new-refresh");

        // Assert
        Assert.Equal("new-access", connection.AccessToken);
        Assert.Equal("new-refresh", connection.RefreshToken);
        Assert.Equal(newExpiry, connection.TokenExpiresAt);
    }

    [Fact]
    public void UpdateTokens_WithNullRefreshToken_PreservesExistingRefreshToken()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        connection.UpdateTokens("new-access", DateTimeOffset.UtcNow.AddHours(2), null);

        Assert.Equal(ValidRefreshToken, connection.RefreshToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTokens_WithInvalidAccessToken_ThrowsArgumentException(string? accessToken)
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.ThrowsAny<ArgumentException>(() =>
            connection.UpdateTokens(accessToken!, DateTimeOffset.UtcNow.AddHours(1)));
    }

    #endregion

    #region IsTokenExpired Tests

    [Fact]
    public void IsTokenExpired_WhenTokenIsFresh_ReturnsFalse()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddHours(1));

        Assert.False(connection.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_WhenTokenExpired_ReturnsTrue()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.True(connection.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_WhenWithinBufferWindow_ReturnsTrue()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(3));

        Assert.True(connection.IsTokenExpired(bufferMinutes: 5));
    }

    [Fact]
    public void IsTokenExpired_WithCustomBuffer_RespectsBufferValue()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(3));

        Assert.False(connection.IsTokenExpired(bufferMinutes: 1));
    }

    [Fact]
    public void IsTokenExpired_WithNegativeBuffer_ThrowsArgumentOutOfRangeException()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddHours(1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            connection.IsTokenExpired(bufferMinutes: -1));
    }

    #endregion

    #region Reassign Tests

    [Fact]
    public void Reassign_WithValidIds_UpdatesUserIdAndProfileId()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);
        var newUserId = Guid.NewGuid();
        var newProfileId = Guid.NewGuid();

        // Act
        connection.Reassign(newUserId, newProfileId);

        // Assert
        Assert.Equal(newUserId, connection.UserId);
        Assert.Equal(newProfileId, connection.ProfileId);
    }

    [Fact]
    public void Reassign_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            connection.Reassign(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Reassign_WithEmptyProfileId_ThrowsArgumentOutOfRangeException()
    {
        var connection = JiraConnection.Create(
            _validUserId, _validProfileId, ValidCloudId, ValidSiteUrl, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            connection.Reassign(Guid.NewGuid(), Guid.Empty));
    }

    #endregion
}
