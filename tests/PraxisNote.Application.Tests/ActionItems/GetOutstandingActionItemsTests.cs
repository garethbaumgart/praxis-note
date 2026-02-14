using NSubstitute;
using PraxisNote.Application.Features.ActionItems;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Tests.ActionItems;

public sealed class GetOutstandingActionItemsTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProfileId = Guid.NewGuid();

    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly ITaskRepository _taskRepo = Substitute.For<ITaskRepository>();
    private readonly GetOutstandingActionItems _sut;

    public GetOutstandingActionItemsTests()
    {
        _sut = new GetOutstandingActionItems(_meetingRepo, _taskRepo);
    }

    #region Empty Data

    [Fact]
    public async Task ExecuteAsync_NoData_ReturnsEmptyList()
    {
        SetupEmptyRepositories();

        var result = await _sut.ExecuteAsync(new GetOutstandingActionItems.Query(UserId, ProfileId));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Outstanding Action Items

    [Fact]
    public async Task ExecuteAsync_OutstandingActionItems_FromLast30Days()
    {
        var recentMeetingDate = DateTimeOffset.UtcNow.AddDays(-2);
        var recentMeeting = Meeting.Create(UserId, ProfileId, "Recent Meeting", recentMeetingDate);
        recentMeeting.SubmitTranscript("Transcript");
        recentMeeting.StartAnalysis();
        recentMeeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Follow up on design") });

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { recentMeeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());

        var result = await _sut.ExecuteAsync(new GetOutstandingActionItems.Query(UserId, ProfileId));

        Assert.Single(result);
        Assert.Equal("Follow up on design", result[0].Description);
        Assert.Equal("Recent Meeting", result[0].MeetingTitle);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedActionItems_ExcludedFromOutstanding()
    {
        var meetingDate = DateTimeOffset.UtcNow.AddDays(-2);
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

        var result = await _sut.ExecuteAsync(new GetOutstandingActionItems.Query(UserId, ProfileId));

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_OldMeetingBeyond30Days_ActionItemsExcluded()
    {
        var oldMeetingDate = DateTimeOffset.UtcNow.AddDays(-45);
        var oldMeeting = Meeting.Create(UserId, ProfileId, "Old Meeting", oldMeetingDate);
        oldMeeting.SubmitTranscript("Transcript");
        oldMeeting.StartAnalysis();
        oldMeeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Old action item") });

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { oldMeeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());

        var result = await _sut.ExecuteAsync(new GetOutstandingActionItems.Query(UserId, ProfileId));

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_LinkedTaskStatus_PropagatedToActionItem()
    {
        var meetingDate = DateTimeOffset.UtcNow.AddDays(-2);
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

        var result = await _sut.ExecuteAsync(new GetOutstandingActionItems.Query(UserId, ProfileId));

        Assert.Single(result);
        var item = result[0];
        Assert.True(item.IsLinkedToTask);
        Assert.Equal(linkedTask.Id, item.LinkedTaskId);
        Assert.Equal("InProgress", item.LinkedTaskStatus);
    }

    [Fact]
    public async Task ExecuteAsync_FutureMeetingActionItems_ExcludedFromOutstanding()
    {
        var futureMeetingDate = DateTimeOffset.UtcNow.AddDays(1);
        var futureMeeting = Meeting.Create(UserId, ProfileId, "Future Meeting", futureMeetingDate);
        futureMeeting.SubmitTranscript("Transcript");
        futureMeeting.StartAnalysis();
        futureMeeting.CompleteAnalysis("Summary", "[]", "[]",
            actionItems: new[] { ActionItem.Create("Future action item") });

        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(new[] { futureMeeting });
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());

        var result = await _sut.ExecuteAsync(new GetOutstandingActionItems.Query(UserId, ProfileId));

        Assert.Empty(result);
    }

    #endregion

    #region Helpers

    private void SetupEmptyRepositories()
    {
        _meetingRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());
        _taskRepo.GetByUserIdAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TaskItem>());
    }

    #endregion
}
