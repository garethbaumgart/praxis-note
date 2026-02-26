using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Domain.Tests.Aggregates;

public class DriveConnectionTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProfileId = Guid.NewGuid();
    private const string ValidProvider = "Google";
    private const string ValidAccessToken = "ya29.access-token";
    private const string ValidRefreshToken = "1//refresh-token";
    private readonly DateTimeOffset _validExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

    #region Create Tests

    [Fact]
    public void Create_WithValidInputs_CreatesConnection()
    {
        // Act
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        // Assert
        Assert.NotEqual(Guid.Empty, connection.Id);
        Assert.Equal(_validUserId, connection.UserId);
        Assert.Equal(_validProfileId, connection.ProfileId);
        Assert.Equal(ValidProvider, connection.Provider);
        Assert.Equal(ValidAccessToken, connection.AccessToken);
        Assert.Equal(ValidRefreshToken, connection.RefreshToken);
        Assert.Equal(_validExpiresAt, connection.TokenExpiresAt);
        Assert.Null(connection.LastSyncedAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DriveConnection.Create(Guid.Empty, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt));
    }

    [Fact]
    public void Create_WithEmptyProfileId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DriveConnection.Create(_validUserId, Guid.Empty, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidProvider_ThrowsArgumentException(string? provider)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            DriveConnection.Create(_validUserId, _validProfileId, provider!, ValidAccessToken, ValidRefreshToken, _validExpiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidAccessToken_ThrowsArgumentException(string? accessToken)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            DriveConnection.Create(_validUserId, _validProfileId, ValidProvider, accessToken!, ValidRefreshToken, _validExpiresAt));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidRefreshToken_ThrowsArgumentException(string? refreshToken)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            DriveConnection.Create(_validUserId, _validProfileId, ValidProvider, ValidAccessToken, refreshToken!, _validExpiresAt));
    }

    [Fact]
    public void Create_TrimsProvider()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, "  Google  ", ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.Equal("Google", connection.Provider);
    }

    [Fact]
    public void Create_FolderIsNullByDefault()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.Null(connection.FolderId);
        Assert.Null(connection.FolderName);
    }

    #endregion

    #region UpdateTokens Tests

    [Fact]
    public void UpdateTokens_WithNewAccessToken_UpdatesAccessTokenAndExpiry()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

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
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        var newExpiry = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        connection.UpdateTokens("new-access", newExpiry, "new-refresh");

        // Assert
        Assert.Equal("new-access", connection.AccessToken);
        Assert.Equal("new-refresh", connection.RefreshToken);
        Assert.Equal(newExpiry, connection.TokenExpiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTokens_WithNullOrWhitespaceRefreshToken_PreservesExistingRefreshToken(string? refreshToken)
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        connection.UpdateTokens("new-access", DateTimeOffset.UtcNow.AddHours(2), refreshToken);

        Assert.Equal(ValidRefreshToken, connection.RefreshToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTokens_WithInvalidAccessToken_ThrowsArgumentException(string? accessToken)
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.ThrowsAny<ArgumentException>(() =>
            connection.UpdateTokens(accessToken!, DateTimeOffset.UtcNow.AddHours(1)));
    }

    #endregion

    #region SetFolder Tests

    [Fact]
    public void SetFolder_WithValidInputs_SetsFolderIdAndName()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        // Act
        connection.SetFolder("folder-123", "Meeting Notes");

        // Assert
        Assert.Equal("folder-123", connection.FolderId);
        Assert.Equal("Meeting Notes", connection.FolderName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetFolder_WithEmptyFolderId_ThrowsArgumentException(string? folderId)
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.ThrowsAny<ArgumentException>(() =>
            connection.SetFolder(folderId!, "Meeting Notes"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetFolder_WithEmptyFolderName_ThrowsArgumentException(string? folderName)
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.ThrowsAny<ArgumentException>(() =>
            connection.SetFolder("folder-123", folderName!));
    }

    #endregion

    #region ClearFolder Tests

    [Fact]
    public void ClearFolder_ResetsToNull()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);
        connection.SetFolder("folder-123", "Meeting Notes");

        // Act
        connection.ClearFolder();

        // Assert
        Assert.Null(connection.FolderId);
        Assert.Null(connection.FolderName);
    }

    #endregion

    #region RecordSync Tests

    [Fact]
    public void RecordSync_SetsLastSyncedAt()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        Assert.Null(connection.LastSyncedAt);

        // Act
        connection.RecordSync();

        // Assert
        Assert.NotNull(connection.LastSyncedAt);
    }

    #endregion

    #region IsTokenExpired Tests

    [Fact]
    public void IsTokenExpired_WhenTokenIsFresh_ReturnsFalse()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddHours(1));

        Assert.False(connection.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_WhenTokenExpired_ReturnsTrue()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.True(connection.IsTokenExpired());
    }

    [Fact]
    public void IsTokenExpired_WhenWithinBufferWindow_ReturnsTrue()
    {
        // Token expires in 3 minutes, but buffer is 5 minutes
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(3));

        Assert.True(connection.IsTokenExpired(bufferMinutes: 5));
    }

    [Fact]
    public void IsTokenExpired_WithCustomBuffer_RespectsBufferValue()
    {
        // Token expires in 3 minutes, buffer is 1 minute
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddMinutes(3));

        Assert.False(connection.IsTokenExpired(bufferMinutes: 1));
    }

    [Fact]
    public void IsTokenExpired_WithNegativeBuffer_ThrowsArgumentOutOfRangeException()
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken,
            DateTimeOffset.UtcNow.AddHours(1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            connection.IsTokenExpired(bufferMinutes: -1));
    }

    #endregion

    #region Reassign Tests

    [Fact]
    public void Reassign_WithValidIds_UpdatesUserIdAndProfileId()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);
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
        // Arrange
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            connection.Reassign(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Reassign_WithEmptyProfileId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            connection.Reassign(Guid.NewGuid(), Guid.Empty));
    }

    #endregion
}
