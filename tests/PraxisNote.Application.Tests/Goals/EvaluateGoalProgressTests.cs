using System.Text.Json;
using NSubstitute;
using PraxisNote.Application.Features.Goals;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.BehavioralGoals;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Tests.Goals;

public sealed class EvaluateGoalProgressTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IBehavioralGoalRepository _goalRepo = Substitute.For<IBehavioralGoalRepository>();
    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly EvaluateGoalProgress _sut;

    public EvaluateGoalProgressTests()
    {
        _sut = new EvaluateGoalProgress(_goalRepo, _meetingRepo);
    }

    [Fact]
    public async Task ExecuteAsync_NoGoals_ReturnsEmptyList()
    {
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BehavioralGoal>());

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_GoalNotMet_ReturnsIsMetFalse()
    {
        var goal = BehavioralGoal.Create(UserId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Talk less");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        var meeting = CreateAnalyzedMeeting(
            CreateAnalysis(talkTime: [("Alice", 40), ("Bob", 60)]));
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Single(result);
        // Bob is target participant (highest avg talk time)
        // Bob talk time = 60, which is NOT less than 50
        Assert.False(result[0].IsMet);
    }

    [Fact]
    public async Task ExecuteAsync_TalkTimeGoalForHighestParticipant_EvaluatesCorrectly()
    {
        // Alice has 40%, goal is < 50 — but target participant is Bob (highest avg)
        var goal = BehavioralGoal.Create(UserId, MetricType.TalkTimePercentage, GoalOperator.LessThanOrEqual, 55, null, "Keep under 55%");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        var meeting = CreateAnalyzedMeeting(
            CreateAnalysis(talkTime: [("Alice", 45), ("Bob", 55)]));
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Single(result);
        Assert.Equal(55, result[0].CurrentValue);
        Assert.True(result[0].IsMet); // 55 <= 55
    }

    [Fact]
    public async Task ExecuteAsync_MultipleGoals_ReturnsProgressForEach()
    {
        var goal1 = BehavioralGoal.Create(UserId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Talk less");
        var goal2 = BehavioralGoal.Create(UserId, MetricType.RedFlagCount, GoalOperator.LessThanOrEqual, 0, null, "Zero flags");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal1, goal2 });

        var analysis = CreateAnalysis(
            talkTime: [("Alice", 60), ("Bob", 40)],
            redFlags: [("evasive", "Alice", "Avoided question", "Context", "high")]);
        var meeting = CreateAnalyzedMeeting(analysis);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ExecuteAsync_StreakCalculation_CountsConsecutiveMetGoals()
    {
        var goal = BehavioralGoal.Create(UserId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Talk less");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        // Alice is target (highest avg across meetings: 55, 55, 45, 45 = avg 50 vs Bob 50)
        // Actually Bob has equal... let's make Alice clearly higher
        var meetings = new[]
        {
            CreateAnalyzedMeeting(CreateAnalysis(talkTime: [("Alice", 60), ("Bob", 40)]), DateTimeOffset.UtcNow.AddDays(-10)),
            CreateAnalyzedMeeting(CreateAnalysis(talkTime: [("Alice", 55), ("Bob", 45)]), DateTimeOffset.UtcNow.AddDays(-7)),
            CreateAnalyzedMeeting(CreateAnalysis(talkTime: [("Alice", 45), ("Bob", 55)]), DateTimeOffset.UtcNow.AddDays(-4)),
            CreateAnalyzedMeeting(CreateAnalysis(talkTime: [("Alice", 40), ("Bob", 60)]), DateTimeOffset.UtcNow.AddDays(-1)),
        };
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Single(result);
        // Target participant is highest average talk time
        // Alice: avg = (60+55+45+40)/4 = 50, Bob: avg = (40+45+55+60)/4 = 50
        // When equal, first alphabetically wins (Alice)
        // Alice's values: 60, 55, 45, 40
        // Goal is < 50, so: fail, fail, pass, pass -> streak = 2
        Assert.Equal(2, result[0].Streak);
    }

    [Fact]
    public async Task ExecuteAsync_RecentResults_ContainsUpToEightEntries()
    {
        var goal = BehavioralGoal.Create(UserId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Test");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        var meetings = Enumerable.Range(0, 10)
            .Select(i => CreateAnalyzedMeeting(
                CreateAnalysis(talkTime: [("Alice", 60), ("Bob", 40)]),
                DateTimeOffset.UtcNow.AddDays(-20 + i)))
            .ToArray();
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Single(result);
        Assert.Equal(8, result[0].RecentResults.Count);
    }

    [Fact]
    public async Task ExecuteAsync_NoMeetings_ReturnsNullCurrentValue()
    {
        var goal = BehavioralGoal.Create(UserId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Test");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal });
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Single(result);
        Assert.Null(result[0].CurrentValue);
        Assert.False(result[0].IsMet);
        Assert.Equal(0, result[0].Streak);
    }

    [Fact]
    public async Task ExecuteAsync_QuestionRatioMetric_ExtractsCorrectValues()
    {
        var goal = BehavioralGoal.Create(UserId, MetricType.QuestionRatio, GoalOperator.GreaterThanOrEqual, 0.3, null, "Ask more questions");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        var analysis = CreateAnalysis(
            talkTime: [("Alice", 60), ("Bob", 40)],
            questionRatios: new Dictionary<string, double> { ["Alice"] = 0.35, ["Bob"] = 0.15 });
        var meeting = CreateAnalyzedMeeting(analysis);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Single(result);
        // Alice is target participant (highest talk time)
        Assert.Equal(0.35, result[0].CurrentValue);
        Assert.True(result[0].IsMet); // 0.35 >= 0.3
    }

    [Fact]
    public async Task ExecuteAsync_RedFlagMetric_CountsCorrectly()
    {
        var goal = BehavioralGoal.Create(UserId, MetricType.RedFlagCount, GoalOperator.LessThanOrEqual, 1, null, "Few red flags");
        _goalRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        var analysis = CreateAnalysis(
            talkTime: [("Alice", 60), ("Bob", 40)],
            redFlags: [
                ("evasive", "Alice", "Avoided question", "Context", "high"),
                ("hedging", "Alice", "Uncertain", "Context", "low"),
                ("defensive", "Bob", "Defensive", "Context", "medium")]);
        var meeting = CreateAnalyzedMeeting(analysis);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new EvaluateGoalProgress.Query(UserId));

        Assert.Single(result);
        // Alice is target, has 2 red flags
        Assert.Equal(2, result[0].CurrentValue);
        Assert.False(result[0].IsMet); // 2 is NOT <= 1
    }

    #region Helpers

    private static Meeting CreateAnalyzedMeeting(BehavioralAnalysisData analysis, DateTimeOffset? date = null)
    {
        var json = JsonSerializer.Serialize(analysis, JsonOptions);
        var meeting = Meeting.Create(UserId, "Test Meeting", date ?? DateTimeOffset.UtcNow.AddDays(-1));
        meeting.SubmitTranscript("Test transcript content for analysis.");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Test summary", "[]", "[]", json);
        return meeting;
    }

    private static BehavioralAnalysisData CreateAnalysis(
        (string name, double pct)[]? talkTime = null,
        Dictionary<string, double>? questionRatios = null,
        (string interrupter, string interrupted, int count)[]? interruptions = null,
        (string participant, string sentiment, double score)[]? sentiments = null,
        (string type, string participant, string description, string context, string severity)[]? redFlags = null)
    {
        var talkTimeList = talkTime?
            .Select(t => new ParticipantTalkTime(t.name, t.pct, $"{t.pct}%"))
            .ToList() ?? [new ParticipantTalkTime("User", 50, "50%")];

        var ratios = questionRatios ?? new Dictionary<string, double>();

        var interruptionPatterns = interruptions?
            .Select(i => new InterruptionPattern(i.interrupter, i.interrupted, i.count))
            .ToList() ?? [];

        var participantSentiments = sentiments?
            .Select(s => new ParticipantSentiment(s.participant, s.sentiment, s.score))
            .ToList() ?? [new ParticipantSentiment("User", "neutral", 0.5)];

        var flags = redFlags?
            .Select(r => new RedFlag(r.type, r.participant, r.description, r.context, r.severity))
            .ToList() ?? [];

        return new BehavioralAnalysisData(
            new SpeakingDynamics(talkTimeList, interruptionPatterns, ratios),
            new SentimentTone(participantSentiments, [], []),
            new CommunicationPatterns(0.8, [], [new ParticipantEngagement("User", "medium", [])]),
            flags);
    }

    #endregion
}
