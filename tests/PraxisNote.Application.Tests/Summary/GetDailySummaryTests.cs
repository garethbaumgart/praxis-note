using NSubstitute;
using PraxisNote.Application.Features.Summary;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Tests.Summary;

public sealed class GetDailySummaryTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProfileId = Guid.NewGuid();

    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly INoteRepository _noteRepo = Substitute.For<INoteRepository>();
    private readonly GetDailySummary _sut;

    public GetDailySummaryTests()
    {
        _sut = new GetDailySummary(_meetingRepo, _taskRepo, _noteRepo);
    }

    #region Empty Day

    [Fact]
    public async Task ExecuteAsync_NoData_ReturnsZeroCounts()
    {
        SetupEmptyRepositories();

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.NotNull(result);
        Assert.Equal(0, result.Stats.MeetingCount);
        Assert.Equal(0, result.Stats.TasksCompleted);
        Assert.Equal(0, result.Stats.TasksStarted);
        Assert.Equal(0, result.Stats.ActionItemsOpen);
        Assert.Equal(0, result.Stats.NotesUpdated);
        Assert.Empty(result.Meetings);
        Assert.Empty(result.OutstandingActionItems);
        Assert.Empty(result.CompletedTasks);
        Assert.Empty(result.InProgressTasks);
        Assert.Empty(result.NotesUpdated);
    }

    [Fact]
    public async Task ExecuteAsync_DataOnDifferentDay_ReturnsZeroCounts()
    {
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
        var meeting = Meeting.Create(UserId, ProfileId, "Yesterday's Meeting", yesterday);

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(0, result.Stats.MeetingCount);
    }

    #endregion

    #region Meetings

    [Fact]
    public async Task ExecuteAsync_MeetingsOnTargetDate_IncludedInResults()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var meetingDate = new DateTimeOffset(2026, 2, 7, 10, 0, 0, TimeSpan.Zero);
        var meeting = Meeting.Create(UserId, ProfileId, "Sprint Planning", meetingDate);

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(1, result.Stats.MeetingCount);
        Assert.Single(result.Meetings);
        Assert.Equal("Sprint Planning", result.Meetings[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_MeetingsOrderedByTime()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var morning = Meeting.Create(UserId, ProfileId, "Morning Standup",
            new DateTimeOffset(2026, 2, 7, 9, 0, 0, TimeSpan.Zero));
        var afternoon = Meeting.Create(UserId, ProfileId, "Afternoon Review",
            new DateTimeOffset(2026, 2, 7, 14, 0, 0, TimeSpan.Zero));

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { afternoon, morning }); // intentionally reversed
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(2, result.Meetings.Count);
        Assert.Equal("Morning Standup", result.Meetings[0].Title);
        Assert.Equal("Afternoon Review", result.Meetings[1].Title);
    }

    [Fact]
    public async Task ExecuteAsync_MeetingWithActionItems_CountsCorrectly()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var meetingDate = new DateTimeOffset(2026, 2, 7, 10, 0, 0, TimeSpan.Zero);
        var meeting = Meeting.Create(UserId, ProfileId, "Planning", meetingDate);

        // Add transcript and analyze to get action items
        meeting.SubmitTranscript("Discussion about project tasks.");
        meeting.StartAnalysis();
        var actionItems = new[]
        {
            ActionItem.Create("Draft API contract", "Alice"),
            ActionItem.Create("Review PR", "Bob"),
        };
        meeting.CompleteAnalysis("Summary of planning", "[]", "[\"Use REST\"]", actionItems: actionItems);

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(2, result.Meetings[0].ActionItemCount);
        Assert.Equal(1, result.Meetings[0].DecisionCount);
        Assert.Equal(0, result.Meetings[0].CompletedActionItemCount);
    }

    [Fact]
    public async Task ExecuteAsync_MeetingWithNullDate_UsesCreatedAtFallback()
    {
        // Meeting created today with null MeetingDate should still appear in today's summary
        var meeting = Meeting.Create(UserId, ProfileId, "Quick Chat", meetingDate: null);

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(1, result.Stats.MeetingCount);
        Assert.Single(result.Meetings);
        Assert.Equal("Quick Chat", result.Meetings[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_MeetingWithNullDecisions_ReturnsZeroDecisionCount()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var meeting = Meeting.Create(UserId, ProfileId, "Planning",
            new DateTimeOffset(2026, 2, 7, 10, 0, 0, TimeSpan.Zero));
        // Meeting in Draft status has null Decisions

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(0, result.Meetings[0].DecisionCount);
    }

    #endregion

    #region Tasks

    [Fact]
    public async Task ExecuteAsync_CompletedTasksOnTargetDate_Included()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var task = TaskItem.CreateStandalone(UserId, ProfileId, "Fix login bug");
        task.Complete(); // Sets CompletedAt to now

        SetupEmptyMeetingsAndNotes();
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { task });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, today));

        Assert.Equal(1, result.Stats.TasksCompleted);
        Assert.Single(result.CompletedTasks);
        Assert.Equal("Fix login bug", result.CompletedTasks[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_InProgressTasksStartedOnDate_Included()
    {
        var task = TaskItem.CreateStandalone(UserId, ProfileId, "Build feature");
        task.Start(); // Sets StartedAt to now, Status = InProgress

        SetupEmptyMeetingsAndNotes();
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { task });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(1, result.Stats.TasksStarted);
        Assert.Single(result.InProgressTasks);
        Assert.Equal("Build feature", result.InProgressTasks[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedTaskNotInProgress_ExcludedFromStarted()
    {
        // A completed task should not appear in InProgress even if StartedAt is today
        var task = TaskItem.CreateStandalone(UserId, ProfileId, "Quick task");
        task.Complete(); // Status = Done, StartedAt = now, CompletedAt = now

        SetupEmptyMeetingsAndNotes();
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { task });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(1, result.Stats.TasksCompleted);
        Assert.Equal(0, result.Stats.TasksStarted); // Not counted as "started" since it's Done
        Assert.Empty(result.InProgressTasks);
    }

    [Fact]
    public async Task ExecuteAsync_PriorityTaskFlagged()
    {
        var task = TaskItem.CreateStandalone(UserId, ProfileId, "Urgent fix");
        task.TogglePriority(); // IsPriority = true
        task.Complete();

        SetupEmptyMeetingsAndNotes();
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { task });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.True(result.CompletedTasks[0].IsPriority);
    }

    #endregion

    #region Notes

    [Fact]
    public async Task ExecuteAsync_NotesUpdatedOnTargetDate_Included()
    {
        var note = Note.Create(UserId, ProfileId, "# My Note\nSome content here");

        SetupEmptyMeetingsAndTasks();
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { note });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(1, result.Stats.NotesUpdated);
        Assert.Single(result.NotesUpdated);
        Assert.True(result.NotesUpdated[0].IsNew); // Created today = new
    }

    [Fact]
    public async Task ExecuteAsync_NoteTitle_ExtractedFromPlainText()
    {
        var note = Note.Create(UserId, ProfileId, "API Design Thoughts\nSome notes about API");

        SetupEmptyMeetingsAndTasks();
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { note });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal("API Design Thoughts", result.NotesUpdated[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyNote_TitledUntitled()
    {
        var note = Note.Create(UserId, ProfileId);

        SetupEmptyMeetingsAndTasks();
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { note });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal("Untitled Note", result.NotesUpdated[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_NoteTitle_ExtractedFromTipTapJson()
    {
        var tipTapJson = """{"type":"doc","content":[{"type":"heading","attrs":{"level":1},"content":[{"type":"text","text":"Sprint Retrospective"}]},{"type":"paragraph","content":[{"type":"text","text":"What went well..."}]}]}""";
        var note = Note.Create(UserId, ProfileId, tipTapJson);

        SetupEmptyMeetingsAndTasks();
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { note });

        var result = await _sut.ExecuteAsync(
            new GetDailySummary.Query(UserId, ProfileId, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal("Sprint Retrospective", result.NotesUpdated[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_EditedNote_IsNewFalse()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        // Create a note that was created yesterday but updated today
        var yesterday = new DateTimeOffset(2026, 2, 6, 10, 0, 0, TimeSpan.Zero);
        var today = new DateTimeOffset(2026, 2, 7, 14, 0, 0, TimeSpan.Zero);
        var note = Note.Create(UserId, ProfileId, "Existing note");
        // Use reflection to set CreatedAt and UpdatedAt for test control
        typeof(Note).GetProperty("CreatedAt")!.SetValue(note, yesterday);
        typeof(Note).GetProperty("UpdatedAt")!.SetValue(note, today);

        SetupEmptyMeetingsAndTasks();
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { note });

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Single(result.NotesUpdated);
        Assert.False(result.NotesUpdated[0].IsNew); // Created yesterday, not new
    }

    #endregion

    #region Outstanding Action Items

    [Fact]
    public async Task ExecuteAsync_OutstandingActionItems_FromLast30Days()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var recentMeetingDate = new DateTimeOffset(2026, 2, 5, 10, 0, 0, TimeSpan.Zero);
        var recentMeeting = Meeting.Create(UserId, ProfileId, "Recent Meeting", recentMeetingDate);
        recentMeeting.SubmitTranscript("Transcript");
        recentMeeting.StartAnalysis();
        recentMeeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Follow up on design") });

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { recentMeeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(1, result.Stats.ActionItemsOpen);
        Assert.Single(result.OutstandingActionItems);
        Assert.Equal("Follow up on design", result.OutstandingActionItems[0].Description);
        Assert.Equal("Recent Meeting", result.OutstandingActionItems[0].MeetingTitle);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedActionItems_ExcludedFromOutstanding()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var meetingDate = new DateTimeOffset(2026, 2, 5, 10, 0, 0, TimeSpan.Zero);
        var meeting = Meeting.Create(UserId, ProfileId, "Meeting", meetingDate);
        meeting.SubmitTranscript("Transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Done item") });

        // Toggle to mark as completed
        var actionItemId = meeting.ActionItems.First().Id;
        meeting.ToggleActionItem(actionItemId);

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(0, result.Stats.ActionItemsOpen);
        Assert.Empty(result.OutstandingActionItems);
    }

    [Fact]
    public async Task ExecuteAsync_OldMeetingBeyond30Days_ActionItemsExcluded()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var oldMeetingDate = new DateTimeOffset(2025, 12, 1, 10, 0, 0, TimeSpan.Zero);
        var oldMeeting = Meeting.Create(UserId, ProfileId, "Old Meeting", oldMeetingDate);
        oldMeeting.SubmitTranscript("Transcript");
        oldMeeting.StartAnalysis();
        oldMeeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Old action item") });

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { oldMeeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(0, result.Stats.ActionItemsOpen);
        Assert.Empty(result.OutstandingActionItems);
    }

    [Fact]
    public async Task ExecuteAsync_LinkedTaskStatus_PropagatedToActionItem()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        var meetingDate = new DateTimeOffset(2026, 2, 5, 10, 0, 0, TimeSpan.Zero);
        var meeting = Meeting.Create(UserId, ProfileId, "Meeting", meetingDate);
        meeting.SubmitTranscript("Transcript");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Task-linked item", "Alice") });

        var actionItemId = meeting.ActionItems.First().Id;

        // Create a task linked to this action item
        var actionItemRef = new ActionItemRef(meeting.Id, actionItemId);
        var linkedTask = TaskItem.CreateFromActionItem(UserId, ProfileId, "Task-linked item", actionItemRef);
        linkedTask.Start(); // Status = InProgress

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { linkedTask });
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Single(result.OutstandingActionItems);
        var item = result.OutstandingActionItems[0];
        Assert.True(item.IsLinkedToTask);
        Assert.Equal(linkedTask.Id, item.LinkedTaskId);
        Assert.Equal("InProgress", item.LinkedTaskStatus);
    }

    [Fact]
    public async Task ExecuteAsync_FutureMeetingActionItems_ExcludedFromOutstanding()
    {
        var targetDate = new DateOnly(2026, 2, 7);
        // Meeting scheduled for the future (after the target date end)
        var futureMeetingDate = new DateTimeOffset(2026, 2, 8, 10, 0, 0, TimeSpan.Zero);
        var futureMeeting = Meeting.Create(UserId, ProfileId, "Future Meeting", futureMeetingDate);
        futureMeeting.SubmitTranscript("Transcript");
        futureMeeting.StartAnalysis();
        futureMeeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Future action item") });

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { futureMeeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(0, result.Stats.ActionItemsOpen);
        Assert.Empty(result.OutstandingActionItems);
    }

    #endregion

    #region Date Returned

    [Fact]
    public async Task ExecuteAsync_ReturnsRequestedDate()
    {
        var targetDate = new DateOnly(2026, 1, 15);
        SetupEmptyRepositories();

        var result = await _sut.ExecuteAsync(new GetDailySummary.Query(UserId, ProfileId, targetDate));

        Assert.Equal(targetDate, result.Date);
    }

    #endregion

    #region Helpers

    private void SetupEmptyRepositories()
    {
        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());
    }

    private void SetupEmptyMeetingsAndNotes()
    {
        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());
        _noteRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Note>());
    }

    private void SetupEmptyMeetingsAndTasks()
    {
        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
    }

    #endregion
}
