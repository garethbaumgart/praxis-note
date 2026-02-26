using System.Text.Json;
using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Tests.Drive;

public class ConfirmDriveImportTests
{
    private readonly IDriveFileImportRepository _driveFileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly IDriveConnectionRepository _driveConnectionRepository = Substitute.For<IDriveConnectionRepository>();
    private readonly IMeetingRepository _meetingRepository = Substitute.For<IMeetingRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfirmDriveImport _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();
    private readonly DriveConnection _connection;

    public ConfirmDriveImportTests()
    {
        _connection = DriveConnection.Create(_userId, _profileId, "Google", "token", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        _driveConnectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(_connection);

        var confirmTranscriptImport = new ConfirmTranscriptImport(
            _meetingRepository, _tagRepository, _unitOfWork, _driveFileImportRepository);
        _sut = new ConfirmDriveImport(confirmTranscriptImport, _driveFileImportRepository, _driveConnectionRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptySelection_ReturnsZeroCounts()
    {
        // Arrange
        var command = new ConfirmDriveImport.Command(_userId, _profileId, []);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.TotalActionItems);
        Assert.Equal(0, result.TagsCreated);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidFiles_CreatesExpectedMeetings()
    {
        // Arrange
        var driveImport = DriveFileImport.Create(_connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        var parsedResult = new
        {
            Title = "Weekly Standup",
            MeetingDate = "2025-01-15T10:00:00+00:00",
            Attendees = "Alice, Bob",
            Summary = "Discussed sprint progress",
            KeyPoints = new[] { "Feature X is done" },
            Decisions = new[] { "Ship next week" },
            ActionItems = new[] { new { Description = "Write docs", Assignee = "Alice" } },
            SuggestedTags = new[] { "standup" },
            Transcript = "Meeting transcript here"
        };
        driveImport.MarkParsed("content", JsonSerializer.Serialize(parsedResult));

        _driveFileImportRepository.GetByIdAsync(driveImport.Id, Arg.Any<CancellationToken>())
            .Returns(driveImport);

        _tagRepository.GetByNamesAsync(_userId, _profileId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag>());

        var command = new ConfirmDriveImport.Command(_userId, _profileId,
        [
            new ConfirmDriveImport.SelectedFile(driveImport.Id, ["standup", "team"])
        ]);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.TotalActionItems);
        Assert.Empty(result.Failures);
        await _meetingRepository.Received(1).AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithEditedTags_UsesUserEditedTags()
    {
        // Arrange
        var driveImport = DriveFileImport.Create(_connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        var parsedResult = new
        {
            Title = "Team Meeting",
            Summary = "Summary",
            SuggestedTags = new[] { "original-tag" },
            Transcript = "text"
        };
        driveImport.MarkParsed("content", JsonSerializer.Serialize(parsedResult));

        _driveFileImportRepository.GetByIdAsync(driveImport.Id, Arg.Any<CancellationToken>())
            .Returns(driveImport);

        var userEditedTags = new List<string> { "custom-tag", "another-tag" };

        _tagRepository.GetByNamesAsync(_userId, _profileId, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag>());

        var command = new ConfirmDriveImport.Command(_userId, _profileId,
        [
            new ConfirmDriveImport.SelectedFile(driveImport.Id, userEditedTags)
        ]);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(1, result.ImportedCount);
        // 2 tags created because "custom-tag" and "another-tag" are new (not existing)
        Assert.Equal(2, result.TagsCreated);
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyImportedFiles_SkipsAndCountsThem()
    {
        // Arrange
        var driveImport = DriveFileImport.Create(_connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        var parsedResult = new { Title = "Test", Summary = "s", Transcript = "t" };
        driveImport.MarkParsed("content", JsonSerializer.Serialize(parsedResult));
        driveImport.MarkImported(Guid.NewGuid());

        _driveFileImportRepository.GetByIdAsync(driveImport.Id, Arg.Any<CancellationToken>())
            .Returns(driveImport);

        var command = new ConfirmDriveImport.Command(_userId, _profileId,
        [
            new ConfirmDriveImport.SelectedFile(driveImport.Id, [])
        ]);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingFile_SkipsIt()
    {
        // Arrange — file is in Pending state (not yet Parsed), should be skipped
        var driveImport = DriveFileImport.Create(_connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);

        _driveFileImportRepository.GetByIdAsync(driveImport.Id, Arg.Any<CancellationToken>())
            .Returns(driveImport);

        var command = new ConfirmDriveImport.Command(_userId, _profileId,
        [
            new ConfirmDriveImport.SelectedFile(driveImport.Id, [])
        ]);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidParsedJson_ReportsFailure()
    {
        // Arrange — file is Parsed but has invalid JSON
        var driveImport = DriveFileImport.Create(_connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        driveImport.MarkParsed("content", "not valid json {{{");

        _driveFileImportRepository.GetByIdAsync(driveImport.Id, Arg.Any<CancellationToken>())
            .Returns(driveImport);

        var command = new ConfirmDriveImport.Command(_userId, _profileId,
        [
            new ConfirmDriveImport.SelectedFile(driveImport.Id, [])
        ]);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Failures);
        Assert.Equal("notes.txt", result.Failures[0].FileName);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentFile_IgnoresIt()
    {
        // Arrange
        var fakeId = Guid.NewGuid();
        _driveFileImportRepository.GetByIdAsync(fakeId, Arg.Any<CancellationToken>())
            .Returns((DriveFileImport?)null);

        var command = new ConfirmDriveImport.Command(_userId, _profileId,
        [
            new ConfirmDriveImport.SelectedFile(fakeId, [])
        ]);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongConnectionId_IgnoresFile()
    {
        // Arrange — file belongs to a different connection
        var otherConnectionId = Guid.NewGuid();
        var driveImport = DriveFileImport.Create(otherConnectionId, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        driveImport.MarkParsed("content", JsonSerializer.Serialize(new { Title = "Test", Transcript = "t" }));

        _driveFileImportRepository.GetByIdAsync(driveImport.Id, Arg.Any<CancellationToken>())
            .Returns(driveImport);

        var command = new ConfirmDriveImport.Command(_userId, _profileId,
        [
            new ConfirmDriveImport.SelectedFile(driveImport.Id, [])
        ]);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Empty(result.Failures);
    }
}
