using NSubstitute;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Tests.Tags;

public class AskTagAiTests
{
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly ITagAiChatService _aiChatService = Substitute.For<ITagAiChatService>();
    private readonly AskTagAi _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _profileId = Guid.NewGuid();
    private readonly Guid _tagId = Guid.NewGuid();

    public AskTagAiTests()
    {
        _sut = new AskTagAi(_tagRepo, _meetingRepo, _noteRepo, _taskRepo, _aiChatService);
    }

    #region Validation

    [Fact]
    public async Task ExecuteAsync_TagNotFound_ThrowsNotFoundError()
    {
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var command = new AskTagAi.Command(_userId, _tagId, "question", null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => { await foreach (var _ in _sut.ExecuteAsync(command)) { } });

        Assert.Equal(AskTagAi.NotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TagBelongsToDifferentUser_ThrowsNotFoundError()
    {
        var otherUserId = Guid.NewGuid();
        var tag = Tag.Create(otherUserId, _profileId, "test-tag");
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns(tag);

        var command = new AskTagAi.Command(_userId, _tagId, "question", null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => { await foreach (var _ in _sut.ExecuteAsync(command)) { } });

        Assert.Equal(AskTagAi.NotFoundError, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_NoContent_ThrowsNoContentError()
    {
        SetupTag();
        SetupEmptyRepositories();

        var command = new AskTagAi.Command(_userId, _tagId, "question", null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => { await foreach (var _ in _sut.ExecuteAsync(command)) { } });

        Assert.Equal(AskTagAi.NoContentError, ex.Message);
    }

    #endregion

    #region Context Building

    [Fact]
    public void BuildContext_WithMeetings_IncludesMeetingContext()
    {
        var meeting = Meeting.Create(_userId, _profileId, "Sprint Planning", attendees: "Alice, Bob");
        var meetings = new List<Meeting> { meeting };

        var context = AskTagAi.BuildContext("project", meetings, new List<Note>(), new List<TaskItem>());

        Assert.Single(context.Meetings);
        Assert.Equal("Sprint Planning", context.Meetings[0].Title);
        Assert.Equal("Alice, Bob", context.Meetings[0].Attendees);
    }

    [Fact]
    public void BuildContext_WithNotes_IncludesNoteContext()
    {
        var note = Note.Create(_userId, _profileId);
        note.UpdateContent("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Test note content"}]}]}""");
        var notes = new List<Note> { note };

        var context = AskTagAi.BuildContext("project", new List<Meeting>(), notes, new List<TaskItem>());

        Assert.Single(context.Notes);
        Assert.Contains("Test note content", context.Notes[0].PlainTextContent);
    }

    [Fact]
    public void BuildContext_WithTasks_IncludesTaskContext()
    {
        var task = TaskItem.CreateStandalone(_userId, _profileId, "Fix bug");
        var tasks = new List<TaskItem> { task };

        var context = AskTagAi.BuildContext("project", new List<Meeting>(), new List<Note>(), tasks);

        Assert.Single(context.Tasks);
        Assert.Equal("Fix bug", context.Tasks[0].Title);
        Assert.Equal("Todo", context.Tasks[0].Status);
    }

    [Fact]
    public void BuildContext_SetsTagName()
    {
        var context = AskTagAi.BuildContext("my-tag", new List<Meeting>(), new List<Note>(), new List<TaskItem>());

        Assert.Equal("my-tag", context.TagName);
    }

    #endregion

    #region Streaming

    [Fact]
    public async Task ExecuteAsync_ValidRequest_StreamsTokens()
    {
        SetupTag();
        SetupSingleTask();

        _aiChatService.StreamResponseAsync(
            Arg.Any<TagChatContext>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<ChatMessage>>(),
            Arg.Any<CancellationToken>())
            .Returns(MockTokenStream("Hello", " world"));

        var command = new AskTagAi.Command(_userId, _tagId, "What tasks are pending?", null);

        var tokens = new List<string>();
        await foreach (var token in _sut.ExecuteAsync(command))
        {
            tokens.Add(token);
        }

        Assert.Equal(2, tokens.Count);
        Assert.Equal("Hello", tokens[0]);
        Assert.Equal(" world", tokens[1]);
    }

    [Fact]
    public async Task ExecuteAsync_WithHistory_PassesHistoryToService()
    {
        SetupTag();
        SetupSingleTask();

        IReadOnlyList<ChatMessage>? capturedHistory = null;
        _aiChatService.StreamResponseAsync(
            Arg.Any<TagChatContext>(),
            Arg.Any<string>(),
            Arg.Do<IReadOnlyList<ChatMessage>>(h => capturedHistory = h),
            Arg.Any<CancellationToken>())
            .Returns(MockTokenStream("response"));

        var history = new List<ChatMessage>
        {
            new("user", "previous question"),
            new("assistant", "previous answer")
        };
        var command = new AskTagAi.Command(_userId, _tagId, "follow up", history);

        await foreach (var _ in _sut.ExecuteAsync(command)) { }

        Assert.NotNull(capturedHistory);
        Assert.Equal(2, capturedHistory!.Count);
        Assert.Equal("previous question", capturedHistory[0].Content);
    }

    #endregion

    #region Helpers

    private void SetupTag()
    {
        var tag = Tag.Create(_userId, _profileId, "test-tag");
        _tagRepo.GetByIdAsync(_tagId, Arg.Any<CancellationToken>()).Returns(tag);
    }

    private void SetupEmptyRepositories()
    {
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
    }

    private void SetupSingleTask()
    {
        _meetingRepo.GetByTagIdAsync(_userId, _profileId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Meeting>());
        _noteRepo.GetByTagIdAsync(_userId, _profileId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<Note>());

        var task = TaskItem.CreateStandalone(_userId, _profileId, "Test task");
        task.AddTag(_tagId);
        _taskRepo.GetTasksWithTagAsync(_userId, _profileId, _tagId, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });
    }

    private static async IAsyncEnumerable<string> MockTokenStream(params string[] tokens)
    {
        foreach (var token in tokens)
        {
            yield return token;
        }
        await Task.CompletedTask; // Suppress compiler warning
    }

    #endregion
}
