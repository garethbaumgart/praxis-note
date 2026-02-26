using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Domain.Tests.Aggregates;

public class DriveConnectionSyncTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProfileId = Guid.NewGuid();
    private const string ValidProvider = "Google";
    private const string ValidAccessToken = "ya29.access-token";
    private const string ValidRefreshToken = "1//refresh-token";
    private readonly DateTimeOffset _validExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

    private DriveConnection CreateConfiguredConnection(int syncFrequencyMinutes = 15)
    {
        var connection = DriveConnection.Create(
            _validUserId, _validProfileId, ValidProvider, ValidAccessToken, ValidRefreshToken, _validExpiresAt);
        connection.Configure("folder-123", "Test Folder", null, syncFrequencyMinutes, false);
        return connection;
    }

    #region IsDueForSync Tests

    [Fact]
    public void IsDueForSync_WhenNeverSynced_ReturnsTrue()
    {
        var connection = CreateConfiguredConnection();

        Assert.True(connection.IsDueForSync());
    }

    [Fact]
    public void IsDueForSync_WhenJustSynced_ReturnsFalse()
    {
        var connection = CreateConfiguredConnection(syncFrequencyMinutes: 15);

        // Just synced — frequency hasn't elapsed yet
        connection.RecordSyncResult(0, 0, 0, 0);

        Assert.False(connection.IsDueForSync());
    }

    [Fact]
    public void IsDueForSync_WhenFrequencyNotElapsed_ReturnsFalse()
    {
        var connection = CreateConfiguredConnection(syncFrequencyMinutes: 60);
        connection.RecordSyncResult(0, 0, 0, 0);

        // Just synced, so 60 minutes haven't passed
        Assert.False(connection.IsDueForSync());
    }

    [Fact]
    public void IsDueForSync_WhenManualOnly_ReturnsFalse()
    {
        var connection = CreateConfiguredConnection(syncFrequencyMinutes: 0);

        // Even though never synced, manual-only connections are never "due"
        Assert.False(connection.IsDueForSync());
    }

    #endregion

    #region IsSyncPaused Tests

    [Fact]
    public void IsSyncPaused_WhenBelowThreshold_ReturnsFalse()
    {
        var connection = CreateConfiguredConnection();

        Assert.False(connection.IsSyncPaused);
    }

    [Fact]
    public void IsSyncPaused_WhenAtThreshold_ReturnsTrue()
    {
        var connection = CreateConfiguredConnection();

        // Simulate 5 consecutive failures
        for (var i = 0; i < 5; i++)
        {
            connection.RecordSyncFailure($"Error {i + 1}");
        }

        Assert.True(connection.IsSyncPaused);
        Assert.Equal(5, connection.ConsecutiveFailures);
    }

    [Fact]
    public void IsSyncPaused_WhenJustBelowThreshold_ReturnsFalse()
    {
        var connection = CreateConfiguredConnection();

        for (var i = 0; i < 4; i++)
        {
            connection.RecordSyncFailure($"Error {i + 1}");
        }

        Assert.False(connection.IsSyncPaused);
        Assert.Equal(4, connection.ConsecutiveFailures);
    }

    #endregion

    #region RecordSyncResult Tests

    [Fact]
    public void RecordSyncResult_UpdatesAllCounters()
    {
        var connection = CreateConfiguredConnection();

        connection.RecordSyncResult(10, 5, 3, 2);

        Assert.NotNull(connection.LastSyncAt);
        Assert.Equal(10, connection.LastSyncFilesDiscovered);
        Assert.Equal(5, connection.LastSyncFilesImported);
        Assert.Equal(3, connection.LastSyncFilesPendingReview);
        Assert.Equal(2, connection.LastSyncFilesErrored);
        Assert.Null(connection.LastSyncError);
    }

    [Fact]
    public void RecordSyncResult_WithSuccessfulImports_ResetsConsecutiveFailures()
    {
        var connection = CreateConfiguredConnection();

        // Accumulate some failures first
        connection.RecordSyncFailure("Error 1");
        connection.RecordSyncFailure("Error 2");
        Assert.Equal(2, connection.ConsecutiveFailures);

        // A successful sync with imports resets the counter
        connection.RecordSyncResult(5, 3, 2, 0);

        Assert.Equal(0, connection.ConsecutiveFailures);
    }

    [Fact]
    public void RecordSyncResult_WithOnlyErrors_IncrementsConsecutiveFailures()
    {
        var connection = CreateConfiguredConnection();

        // First: a sync with only errors and no imports/reviews
        connection.RecordSyncResult(5, 0, 0, 5);

        Assert.Equal(1, connection.ConsecutiveFailures);

        // Second: another all-error sync
        connection.RecordSyncResult(3, 0, 0, 3);

        Assert.Equal(2, connection.ConsecutiveFailures);
    }

    [Fact]
    public void RecordSyncResult_WithMixedResults_ResetsConsecutiveFailures()
    {
        var connection = CreateConfiguredConnection();
        connection.RecordSyncFailure("Error");
        Assert.Equal(1, connection.ConsecutiveFailures);

        // A sync with some errors but also some pending review files resets failures
        connection.RecordSyncResult(5, 0, 2, 3);

        Assert.Equal(0, connection.ConsecutiveFailures);
    }

    [Fact]
    public void RecordSyncResult_ClearsLastSyncError()
    {
        var connection = CreateConfiguredConnection();
        connection.RecordSyncFailure("Previous error");
        Assert.NotNull(connection.LastSyncError);

        connection.RecordSyncResult(5, 3, 2, 0);

        Assert.Null(connection.LastSyncError);
    }

    #endregion

    #region RecordSyncFailure Tests

    [Fact]
    public void RecordSyncFailure_SetsErrorMessage()
    {
        var connection = CreateConfiguredConnection();

        connection.RecordSyncFailure("OAuth token expired");

        Assert.Equal("OAuth token expired", connection.LastSyncError);
        Assert.NotNull(connection.LastSyncAt);
    }

    [Fact]
    public void RecordSyncFailure_IncrementsConsecutiveFailures()
    {
        var connection = CreateConfiguredConnection();

        connection.RecordSyncFailure("Error 1");
        Assert.Equal(1, connection.ConsecutiveFailures);

        connection.RecordSyncFailure("Error 2");
        Assert.Equal(2, connection.ConsecutiveFailures);
    }

    [Fact]
    public void RecordSyncFailure_TrimsErrorMessage()
    {
        var connection = CreateConfiguredConnection();

        connection.RecordSyncFailure("  Error with spaces  ");

        Assert.Equal("Error with spaces", connection.LastSyncError);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordSyncFailure_WithInvalidMessage_ThrowsArgumentException(string? message)
    {
        var connection = CreateConfiguredConnection();

        Assert.ThrowsAny<ArgumentException>(() =>
            connection.RecordSyncFailure(message!));
    }

    #endregion

    #region ClearSyncError Tests

    [Fact]
    public void ClearSyncError_ResetsErrorAndFailureCount()
    {
        var connection = CreateConfiguredConnection();
        connection.RecordSyncFailure("Error 1");
        connection.RecordSyncFailure("Error 2");
        connection.RecordSyncFailure("Error 3");

        Assert.Equal("Error 3", connection.LastSyncError);
        Assert.Equal(3, connection.ConsecutiveFailures);

        connection.ClearSyncError();

        Assert.Null(connection.LastSyncError);
        Assert.Equal(0, connection.ConsecutiveFailures);
    }

    [Fact]
    public void ClearSyncError_WhenNoErrors_DoesNothing()
    {
        var connection = CreateConfiguredConnection();

        connection.ClearSyncError();

        Assert.Null(connection.LastSyncError);
        Assert.Equal(0, connection.ConsecutiveFailures);
    }

    #endregion
}
