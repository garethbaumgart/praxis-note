using System.Text.Json;
using NSubstitute;
using PraxisNote.Application.Features.Insights;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Tests.Insights;

public sealed class GetInsightsSummaryTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly GetInsightsSummary _sut;

    public GetInsightsSummaryTests()
    {
        _sut = new GetInsightsSummary(_meetingRepo);
    }

    #region No Data

    [Fact]
    public async Task ExecuteAsync_NoMeetings_ReturnsNull()
    {
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyDraftMeetings_ReturnsNull()
    {
        var meeting = Meeting.Create(UserId, "Draft meeting");
        // Draft meetings have no behavioral analysis
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_MeetingsWithInvalidJson_ReturnsNull()
    {
        var meeting = CreateAnalyzedMeeting("not valid json");

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.Null(result);
    }

    #endregion

    #region With Data

    [Fact]
    public async Task ExecuteAsync_SingleMeeting_ReturnsSummaryWithCorrectMetrics()
    {
        var analysis = CreateAnalysis(
            talkTime: [("Alice", 42), ("Bob", 58)],
            questionRatios: new Dictionary<string, double> { ["Alice"] = 0.35, ["Bob"] = 0.2 },
            sentiments: [("Alice", "positive", 0.7), ("Bob", "neutral", 0.5)],
            redFlags: [("hedging", "Bob", "Used uncertain language", "Budget discussion", "medium")]);

        var meeting = CreateAnalyzedMeeting(Serialize(analysis));

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.NotNull(result);
        Assert.Equal(1, result.MeetingCount);
        // Bob has higher talk time so is the target participant
        Assert.Equal("Bob", result.ParticipantName);
        Assert.Equal(58, result.Headline.Value);
        Assert.Equal("%", result.Headline.Unit);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleMeetings_CalculatesAverages()
    {
        var analysis1 = CreateAnalysis(
            talkTime: [("Alice", 40), ("Bob", 60)],
            questionRatios: new Dictionary<string, double> { ["Alice"] = 0.3, ["Bob"] = 0.2 });

        var analysis2 = CreateAnalysis(
            talkTime: [("Alice", 50), ("Bob", 50)],
            questionRatios: new Dictionary<string, double> { ["Alice"] = 0.5, ["Bob"] = 0.4 });

        var meeting1 = CreateAnalyzedMeeting(Serialize(analysis1), DateTimeOffset.UtcNow.AddDays(-10));
        var meeting2 = CreateAnalyzedMeeting(Serialize(analysis2), DateTimeOffset.UtcNow.AddDays(-5));

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting1, meeting2 });

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.NotNull(result);
        Assert.Equal(2, result.MeetingCount);
        // Average talk time for Alice: (40+50)/2 = 45
        // Average talk time for Bob: (60+50)/2 = 55 — Bob is still highest
        Assert.Equal("Bob", result.ParticipantName);
        Assert.Equal(55, result.Headline.Value);
    }

    [Fact]
    public async Task ExecuteAsync_SparklineContainsUpToEightValues()
    {
        var meetings = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var analysis = CreateAnalysis(
                    talkTime: [("Alice", 30 + i), ("Bob", 70 - i)]);
                return CreateAnalyzedMeeting(Serialize(analysis), DateTimeOffset.UtcNow.AddDays(-20 + i));
            })
            .ToArray();

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.NotNull(result);
        Assert.Equal(8, result.SparklineValues.Count);
    }

    [Fact]
    public async Task ExecuteAsync_RedFlagsCounted_ForTargetParticipant()
    {
        var analysis = CreateAnalysis(
            talkTime: [("Alice", 60), ("Bob", 40)],
            redFlags: [
                ("evasive", "Alice", "Avoided question", "Context", "high"),
                ("hedging", "Alice", "Used uncertain language", "Context", "low"),
                ("defensive", "Bob", "Became defensive", "Context", "medium")]);

        var meeting = CreateAnalyzedMeeting(Serialize(analysis));

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.NotNull(result);
        Assert.Equal("Alice", result.ParticipantName);
        Assert.Equal(2, result.RedFlags.Value);
    }

    [Fact]
    public async Task ExecuteAsync_OldMeetingsBeyond30Days_Excluded()
    {
        var recentAnalysis = CreateAnalysis(talkTime: [("Alice", 50), ("Bob", 50)]);
        var oldAnalysis = CreateAnalysis(talkTime: [("Alice", 80), ("Bob", 20)]);

        var recentMeeting = CreateAnalyzedMeeting(Serialize(recentAnalysis), DateTimeOffset.UtcNow.AddDays(-5));
        var oldMeeting = CreateAnalyzedMeeting(Serialize(oldAnalysis), DateTimeOffset.UtcNow.AddDays(-60));

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { recentMeeting, oldMeeting });

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.NotNull(result);
        Assert.Equal(1, result.MeetingCount);
    }

    #endregion

    #region Nudge Text

    [Fact]
    public async Task ExecuteAsync_QuestionRatioImproving_ReturnsNudge()
    {
        // Create meetings where question ratio clearly improves
        var meetings = new List<Meeting>();
        for (int i = 0; i < 6; i++)
        {
            var ratio = i < 3 ? 0.1 : 0.5; // big jump in second half
            var analysis = CreateAnalysis(
                talkTime: [("Alice", 50), ("Bob", 50)],
                questionRatios: new Dictionary<string, double> { ["Alice"] = ratio, ["Bob"] = 0.2 });
            meetings.Add(CreateAnalyzedMeeting(Serialize(analysis), DateTimeOffset.UtcNow.AddDays(-20 + i * 3)));
        }

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetInsightsSummary.Query(UserId));

        Assert.NotNull(result);
        Assert.NotNull(result.NudgeText);
        Assert.Contains("question ratio", result.NudgeText, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Helpers

    private static Meeting CreateAnalyzedMeeting(string behavioralAnalysisJson, DateTimeOffset? date = null)
    {
        var meeting = Meeting.Create(UserId, "Test Meeting", date ?? DateTimeOffset.UtcNow.AddDays(-1));
        meeting.SubmitTranscript("Test transcript content for analysis.");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Test summary", "[]", "[]", behavioralAnalysisJson);
        return meeting;
    }

    private static string Serialize(BehavioralAnalysisData data)
        => JsonSerializer.Serialize(data, JsonOptions);

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
