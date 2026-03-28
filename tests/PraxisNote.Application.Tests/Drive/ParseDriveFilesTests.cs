using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.UserAiKeys;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Tests.Drive;

public class ParseDriveFilesTests
{
    private readonly IDriveConnectionRepository _connectionRepository = Substitute.For<IDriveConnectionRepository>();
    private readonly IDriveFileImportRepository _fileImportRepository = Substitute.For<IDriveFileImportRepository>();
    private readonly IDriveService _driveService = Substitute.For<IDriveService>();
    private readonly ParseTranscriptForImport _parseTranscript;
    private readonly ITranscriptExtractor _transcriptExtractor = Substitute.For<ITranscriptExtractor>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ParseDriveFiles> _logger = Substitute.For<ILogger<ParseDriveFiles>>();
    private readonly IMeetingAnalyzer _meetingAnalyzer = Substitute.For<IMeetingAnalyzer>();
    private readonly ParseDriveFiles _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();

    public ParseDriveFilesTests()
    {
        var aiServices = Substitute.For<IResolvedAiServices>();
        aiServices.GetMeetingAnalyzerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_meetingAnalyzer);
        _parseTranscript = new ParseTranscriptForImport(aiServices, _transcriptExtractor);
        _sut = new ParseDriveFiles(
            _connectionRepository, _fileImportRepository, _driveService,
            _parseTranscript, _transcriptExtractor, _userRepository, _unitOfWork, _logger);
    }

    private DriveConnection CreateConnectionWithFolder(DateTimeOffset? tokenExpiresAt = null, string? timeZone = null)
    {
        var connection = DriveConnection.Create(
            _userId, _profileId, "Google", "access-token", "refresh-token",
            tokenExpiresAt ?? DateTimeOffset.UtcNow.AddHours(1));
        connection.Configure("folder-123", "Meeting Notes", null, 15, false, timeZone);
        return connection;
    }

    private void SetupMeetingAnalyzer(string? title = "Test Meeting", string? attendees = null)
    {
        _meetingAnalyzer.ParseTranscriptForImportAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptImportResult(
                title,
                DateTimeOffset.UtcNow,
                attendees,
                "Summary of the meeting",
                ["Key point 1"],
                ["Decision 1"],
                [new ExtractedActionItem("Action 1", null)],
                ["tag1"],
                true,
                null));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns((DriveConnection?)null);

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredToken_RefreshesBeforeParsing()
    {
        // Arrange
        var connection = CreateConnectionWithFolder(tokenExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var refreshResult = new TokenRefreshResult("new-access", DateTimeOffset.UtcNow.AddHours(1), null);
        _driveService.RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(refreshResult);

        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport>());

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert
        await _driveService.Received(1).RefreshAccessTokenAsync("refresh-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingPlainTextFile_DownloadsAndParses()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var fileImport = DriveFileImport.Create(connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { fileImport });

        var stream = new MemoryStream("Meeting transcript text"u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("Meeting transcript text");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(1, result.Parsed);
        Assert.Equal(0, result.Errors);
        Assert.Equal(DriveFileImportStatus.Parsed, fileImport.Status);
        Assert.NotNull(fileImport.ParsedResultJson);
    }

    [Fact]
    public async Task ExecuteAsync_WithGoogleDoc_ExportsAsPlainText()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var fileImport = DriveFileImport.Create(
            connection.Id, "doc-1", "Meeting Notes", "application/vnd.google-apps.document", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { fileImport });

        _driveService.ExportGoogleDocAsync("access-token", "doc-1", Arg.Any<CancellationToken>())
            .Returns("Exported document text");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(1, result.Parsed);
        await _driveService.Received(1).ExportGoogleDocAsync("access-token", "doc-1", Arg.Any<CancellationToken>());
        await _driveService.DidNotReceive().DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithDocx_DownloadsAndExtractsText()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        const string docxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        var fileImport = DriveFileImport.Create(
            connection.Id, "docx-1", "report.docx", docxMime, DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { fileImport });

        var stream = new MemoryStream([0x50, 0x4B]); // Fake docx bytes
        _driveService.DownloadFileAsync("access-token", "docx-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromDocxAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("Extracted docx text");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(1, result.Parsed);
        await _driveService.Received(1).DownloadFileAsync("access-token", "docx-1", Arg.Any<CancellationToken>());
        await _transcriptExtractor.Received(1).ExtractTextFromDocxAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyFile_MarksAsSkipped()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var fileImport = DriveFileImport.Create(connection.Id, "file-1", "empty.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { fileImport });

        var stream = new MemoryStream(""u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("   ");

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.Parsed);
        Assert.Equal(0, result.Errors);
        Assert.Equal(DriveFileImportStatus.Skipped, fileImport.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithParseError_MarksAsErrorAndContinues()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var failingFile = DriveFileImport.Create(connection.Id, "file-1", "bad.txt", "text/plain", DateTimeOffset.UtcNow);
        var goodFile = DriveFileImport.Create(connection.Id, "file-2", "good.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { failingFile, goodFile });

        // First file: download fails
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Download error"));

        // Second file: succeeds
        var stream = new MemoryStream("Good content"u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-2", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("Good content");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(1, result.Parsed);
        Assert.Equal(1, result.Errors);
        Assert.Equal(DriveFileImportStatus.Error, failingFile.Status);
        Assert.Equal("Download error", failingFile.ErrorMessage);
        Assert.Equal(DriveFileImportStatus.Parsed, goodFile.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithMoreThan50Files_ProcessesOnlyFirst50()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var pendingFiles = Enumerable.Range(1, 60)
            .Select(i => DriveFileImport.Create(connection.Id, $"file-{i}", $"notes-{i}.txt", "text/plain", DateTimeOffset.UtcNow))
            .ToList();
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(pendingFiles);

        // All files succeed
        _driveService.DownloadFileAsync("access-token", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream("content"u8.ToArray()));
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(50, result.Parsed);
        Assert.Equal(0, result.Errors);
        Assert.Equal(10, result.Remaining);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsRemainingCount()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var pendingFiles = Enumerable.Range(1, 5)
            .Select(i => DriveFileImport.Create(connection.Id, $"file-{i}", $"notes-{i}.txt", "text/plain", DateTimeOffset.UtcNow))
            .ToList();
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(pendingFiles);

        _driveService.DownloadFileAsync("access-token", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream("content"u8.ToArray()));
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(5, result.Parsed);
        Assert.Equal(0, result.Remaining);
    }

    [Fact]
    public async Task ExecuteAsync_SavesAfterEachFile()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var files = new List<DriveFileImport>
        {
            DriveFileImport.Create(connection.Id, "file-1", "a.txt", "text/plain", DateTimeOffset.UtcNow),
            DriveFileImport.Create(connection.Id, "file-2", "b.txt", "text/plain", DateTimeOffset.UtcNow),
            DriveFileImport.Create(connection.Id, "file-3", "c.txt", "text/plain", DateTimeOffset.UtcNow),
        };
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(files);

        _driveService.DownloadFileAsync("access-token", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream("content"u8.ToArray()));
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert — SaveChangesAsync called once per file (3 files) = 3 calls
        await _unitOfWork.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesUserNameAndTimezone()
    {
        // Arrange
        var connection = CreateConnectionWithFolder(timeZone: "America/New_York");
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var user = User.Register(new ExternalIdentity("Google", "ext-id"), new Email("test@example.com"), "Jane Doe");
        _userRepository.GetByIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var fileImport = DriveFileImport.Create(connection.Id, "file-1", "notes.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { fileImport });

        var stream = new MemoryStream("Meeting text"u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("Meeting text");

        SetupMeetingAnalyzer();

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        await _sut.ExecuteAsync(command);

        // Assert — verify the meeting analyzer received the timezone
        await _meetingAnalyzer.Received(1).ParseTranscriptForImportAsync(
            Arg.Any<string>(),
            "America/New_York",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPendingFiles_ReturnsZeroParsed()
    {
        // Arrange
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport>());

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        // Act
        var result = await _sut.ExecuteAsync(command);

        // Assert
        Assert.Equal(0, result.Parsed);
        Assert.Equal(0, result.Errors);
        Assert.Equal(0, result.Remaining);
    }

    #region AI exception bubbling

    [Fact]
    public async Task ExecuteAsync_AiKeyInvalidException_BubblesUpInsteadOfMarkingFileError()
    {
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var file = DriveFileImport.Create(connection.Id, "file-1", "doc.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { file });

        var stream = new MemoryStream("content"u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        _meetingAnalyzer.ParseTranscriptForImportAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new AiKeyInvalidException("Gemini"));

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        await Assert.ThrowsAsync<AiKeyInvalidException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_AiRateLimitedException_BubblesUp()
    {
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var file = DriveFileImport.Create(connection.Id, "file-1", "doc.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { file });

        var stream = new MemoryStream("content"u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        _meetingAnalyzer.ParseTranscriptForImportAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new AiRateLimitedException("Gemini", 30));

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        var ex = await Assert.ThrowsAsync<AiRateLimitedException>(() => _sut.ExecuteAsync(command));
        Assert.Equal(30, ex.RetryAfterSeconds);
    }

    [Fact]
    public async Task ExecuteAsync_AiProviderException_BubblesUp()
    {
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var file = DriveFileImport.Create(connection.Id, "file-1", "doc.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { file });

        var stream = new MemoryStream("content"u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        _meetingAnalyzer.ParseTranscriptForImportAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new AiProviderException("Gemini", "Gemini returned an error."));

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        await Assert.ThrowsAsync<AiProviderException>(() => _sut.ExecuteAsync(command));
    }

    [Fact]
    public async Task ExecuteAsync_NoAiKeyConfiguredException_BubblesUp()
    {
        var connection = CreateConnectionWithFolder();
        _connectionRepository.GetByUserIdAsync(_userId, _profileId, Arg.Any<CancellationToken>())
            .Returns(connection);

        var file = DriveFileImport.Create(connection.Id, "file-1", "doc.txt", "text/plain", DateTimeOffset.UtcNow);
        _fileImportRepository.GetByStatusAsync(connection.Id, DriveFileImportStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<DriveFileImport> { file });

        var stream = new MemoryStream("content"u8.ToArray());
        _driveService.DownloadFileAsync("access-token", "file-1", Arg.Any<CancellationToken>())
            .Returns(stream);
        _transcriptExtractor.ExtractTextFromPlainText(Arg.Any<Stream>())
            .Returns("content");

        _meetingAnalyzer.ParseTranscriptForImportAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new NoAiKeyConfiguredException());

        var command = new ParseDriveFiles.Command(_userId, _profileId);

        await Assert.ThrowsAsync<NoAiKeyConfiguredException>(() => _sut.ExecuteAsync(command));
    }

    #endregion
}
