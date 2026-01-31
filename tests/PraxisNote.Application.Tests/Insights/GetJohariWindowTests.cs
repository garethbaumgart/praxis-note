using System.Text.Json;
using NSubstitute;
using PraxisNote.Application.Features.Insights;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Tests.Insights;

public sealed class GetJohariWindowTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMeetingRepository _meetingRepo = Substitute.For<IMeetingRepository>();
    private readonly GetJohariWindow _sut;

    public GetJohariWindowTests()
    {
        _sut = new GetJohariWindow(_meetingRepo);
    }

    #region Insufficient Data

    [Fact]
    public async Task ExecuteAsync_NoMeetings_ReturnsHasEnoughDataFalse()
    {
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.False(result.HasEnoughData);
        Assert.Equal(0, result.MeetingCount);
        Assert.Equal(GetJohariWindow.MinimumMeetings, result.MinimumMeetings);
        Assert.Empty(result.Dimensions);
        Assert.Empty(result.BlindSpots);
    }

    [Fact]
    public async Task ExecuteAsync_TwoMeetings_ReturnsHasEnoughDataFalse()
    {
        var meetings = CreateMeetingsWithReflections(2);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.False(result.HasEnoughData);
        Assert.Equal(2, result.MeetingCount);
    }

    [Fact]
    public async Task ExecuteAsync_MeetingsWithoutReflections_ReturnsHasEnoughDataFalse()
    {
        // 5 meetings with analysis but no reflection
        var meetings = Enumerable.Range(0, 5)
            .Select(i => CreateAnalyzedMeeting(
                CreateDefaultAnalysisJson(),
                reflectionJson: null,
                DateTimeOffset.UtcNow.AddDays(-10 + i)))
            .ToArray();

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.False(result.HasEnoughData);
    }

    [Fact]
    public async Task ExecuteAsync_DraftMeetings_ReturnsHasEnoughDataFalse()
    {
        var meetings = Enumerable.Range(0, 5)
            .Select(_ => Meeting.Create(UserId, "Draft"))
            .ToArray();

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.False(result.HasEnoughData);
        Assert.Equal(0, result.MeetingCount);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidAnalysisJson_ExcludesMeeting()
    {
        // 2 valid + 2 invalid = only 2 valid → not enough
        var validMeetings = CreateMeetingsWithReflections(2);
        var invalidMeetings = Enumerable.Range(0, 2)
            .Select(i => CreateAnalyzedMeeting(
                "not valid json",
                CreateDefaultReflectionJson(),
                DateTimeOffset.UtcNow.AddDays(-5 + i)))
            .ToArray();

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(validMeetings.Concat(invalidMeetings).ToArray());

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.False(result.HasEnoughData);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyResult_AllPercentagesZero()
    {
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Meeting>());

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.Equal(0, result.OpenPercentage);
        Assert.Equal(0, result.BlindSpotPercentage);
        Assert.Equal(0, result.HiddenPercentage);
        Assert.Equal(0, result.UnknownPercentage);
        Assert.Null(result.OpenTrend);
    }

    #endregion

    #region Classification — Talk Time

    [Theory]
    [InlineData(40, 40, "Open")]      // Exact match
    [InlineData(50, 40, "Open")]      // Within 15 tolerance (diff=10)
    [InlineData(25, 40, "Open")]      // Within 15 tolerance (diff=15)
    [InlineData(60, 40, "BlindSpot")] // Off by 20
    [InlineData(10, 40, "BlindSpot")] // Off by 30
    [InlineData(null, 40, "Unknown")] // No self-assessment
    public void ClassifyTalkTime_VariousInputs_CorrectQuadrant(int? selfAssessed, double actual, string expected)
    {
        var result = GetJohariWindow.ClassifyTalkTime(selfAssessed, actual);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Classification — Engagement

    [Theory]
    [InlineData("Highly Engaged", "high", "Open")]     // Both map to 3
    [InlineData("Moderate", "medium", "Open")]           // Both map to 2
    [InlineData("Disengaged", "low", "Open")]            // Both map to 1
    [InlineData("Highly Engaged", "low", "BlindSpot")]   // 3 vs 1
    [InlineData("Disengaged", "high", "BlindSpot")]      // 1 vs 3
    [InlineData(null, "high", "Unknown")]                 // No self-assessment
    [InlineData("Moderate", null, "Unknown")]             // No AI data
    public void ClassifyEngagement_VariousInputs_CorrectQuadrant(string? self, string? actual, string expected)
    {
        var result = GetJohariWindow.ClassifyEngagement(self, actual);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Classification — Tone

    [Theory]
    [InlineData("Collaborative", 0.8, "Open")]       // ≥ 0.6
    [InlineData("Collaborative", 0.6, "Open")]        // Exactly 0.6
    [InlineData("Collaborative", 0.3, "BlindSpot")]   // < 0.6
    [InlineData("Neutral", 0.5, "Open")]              // 0.35–0.65
    [InlineData("Neutral", 0.35, "Open")]             // Lower bound
    [InlineData("Neutral", 0.65, "Open")]             // Upper bound
    [InlineData("Neutral", 0.1, "BlindSpot")]         // Below range
    [InlineData("Neutral", 0.9, "BlindSpot")]         // Above range
    [InlineData("Tense", 0.2, "Open")]                // ≤ 0.4
    [InlineData("Tense", 0.4, "Open")]                // Exactly 0.4
    [InlineData("Tense", 0.8, "BlindSpot")]           // > 0.4
    [InlineData(null, 0.5, "Unknown")]                 // No self-assessment
    [InlineData("nonsense", 0.5, "Unknown")]           // Unrecognized value
    public void ClassifyTone_VariousInputs_CorrectQuadrant(string? self, double sentimentScore, string expected)
    {
        var result = GetJohariWindow.ClassifyTone(self, sentimentScore);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Classification — Interruptions

    [Theory]
    [InlineData("Yes", 3, "Open")]         // Aware + actually interrupted
    [InlineData("Yes", 0, "BlindSpot")]    // Thought they did, but didn't
    [InlineData("No", 0, "Open")]          // Correct: no interruptions
    [InlineData("No", 2, "BlindSpot")]     // Unaware of interruptions
    [InlineData("Partially", 1, "Open")]   // Partially aware, ≤ 2
    [InlineData("Partially", 2, "Open")]   // Partially aware, ≤ 2
    [InlineData("Partially", 5, "BlindSpot")] // Partially aware, > 2
    [InlineData(null, 3, "Unknown")]       // No self-assessment
    [InlineData("other", 1, "Unknown")]    // Unrecognized value
    public void ClassifyInterruptions_VariousInputs_CorrectQuadrant(string? self, int actual, string expected)
    {
        var result = GetJohariWindow.ClassifyInterruptions(self, actual);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Hidden Count

    [Fact]
    public void CalculateHiddenCount_LongFreeformReflection_ReturnsOne()
    {
        var reflection = new ReflectionDto(null, null, null, null,
            "This is a long freeform reflection that exceeds twenty characters",
            []);

        Assert.Equal(1, GetJohariWindow.CalculateHiddenCount(reflection));
    }

    [Fact]
    public void CalculateHiddenCount_ShortFreeformReflection_ReturnsZero()
    {
        var reflection = new ReflectionDto(null, null, null, null, "Short", []);

        Assert.Equal(0, GetJohariWindow.CalculateHiddenCount(reflection));
    }

    [Fact]
    public void CalculateHiddenCount_NullFreeformReflection_ReturnsZero()
    {
        var reflection = new ReflectionDto(null, null, null, null, null, []);

        Assert.Equal(0, GetJohariWindow.CalculateHiddenCount(reflection));
    }

    [Fact]
    public void CalculateHiddenCount_EmptyFreeformReflection_ReturnsZero()
    {
        var reflection = new ReflectionDto(null, null, null, null, "", []);

        Assert.Equal(0, GetJohariWindow.CalculateHiddenCount(reflection));
    }

    #endregion

    #region Full Pipeline — Aggregation

    [Fact]
    public async Task ExecuteAsync_ThreeMeetings_ReturnsHasEnoughDataTrue()
    {
        var meetings = CreateMeetingsWithReflections(3);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.True(result.HasEnoughData);
        Assert.Equal(3, result.MeetingCount);
    }

    [Fact]
    public async Task ExecuteAsync_ThreeAlignedMeetings_HighOpenPercentage()
    {
        // Create meetings where self-assessment matches AI analysis
        var meetings = CreateAlignedMeetings(3);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.True(result.HasEnoughData);
        Assert.True(result.OpenPercentage > 50, $"Expected Open > 50%, got {result.OpenPercentage}%");
    }

    [Fact]
    public async Task ExecuteAsync_ThreeMisalignedMeetings_HighBlindSpotPercentage()
    {
        // Create meetings where self-assessment is way off from AI analysis
        var meetings = CreateMisalignedMeetings(3);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.True(result.HasEnoughData);
        Assert.True(result.BlindSpotPercentage > 50, $"Expected BlindSpot > 50%, got {result.BlindSpotPercentage}%");
    }

    [Fact]
    public async Task ExecuteAsync_PercentagesSumTo100()
    {
        var meetings = CreateMeetingsWithReflections(5);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        var total = result.OpenPercentage + result.BlindSpotPercentage +
                    result.HiddenPercentage + result.UnknownPercentage;
        Assert.Equal(100, total);
    }

    [Fact]
    public async Task ExecuteAsync_WithData_ReturnsFourDimensions()
    {
        var meetings = CreateMeetingsWithReflections(3);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.Equal(4, result.Dimensions.Count);
        var names = result.Dimensions.Select(d => d.Name).ToHashSet();
        Assert.Contains("Talk Time", names);
        Assert.Contains("Engagement", names);
        Assert.Contains("Tone", names);
        Assert.Contains("Interruptions", names);
    }

    [Fact]
    public async Task ExecuteAsync_DimensionsHaveValidQuadrants()
    {
        var meetings = CreateMeetingsWithReflections(3);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        var validQuadrants = new HashSet<string> { "Open", "BlindSpot", "Hidden", "Unknown" };
        foreach (var dim in result.Dimensions)
        {
            Assert.Contains(dim.Quadrant, validQuadrants);
            Assert.NotNull(dim.Explanation);
        }
    }

    [Fact]
    public async Task ExecuteAsync_MisalignedMeetings_ReturnsBlindSpotDetails()
    {
        var meetings = CreateMisalignedMeetings(3);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.NotEmpty(result.BlindSpots);
        foreach (var spot in result.BlindSpots)
        {
            Assert.NotEmpty(spot.Dimension);
            Assert.NotEmpty(spot.Description);
            Assert.True(spot.MeetingCount > 0);
        }
    }

    #endregion

    #region Date Range Filtering

    [Fact]
    public async Task ExecuteAsync_RangeFilter_ExcludesOldMeetings()
    {
        // 3 meetings within range, 2 outside
        var recentMeetings = Enumerable.Range(0, 3)
            .Select(i => CreateAnalyzedMeeting(
                CreateDefaultAnalysisJson(),
                CreateDefaultReflectionJson(),
                DateTimeOffset.UtcNow.AddDays(-5 + i)))
            .ToArray();

        var oldMeetings = Enumerable.Range(0, 2)
            .Select(i => CreateAnalyzedMeeting(
                CreateDefaultAnalysisJson(),
                CreateDefaultReflectionJson(),
                DateTimeOffset.UtcNow.AddDays(-60 + i)))
            .ToArray();

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(recentMeetings.Concat(oldMeetings).ToArray());

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "7d"));

        Assert.True(result.HasEnoughData);
        Assert.Equal(3, result.MeetingCount);
    }

    [Fact]
    public async Task ExecuteAsync_AllRange_IncludesEverything()
    {
        var oldMeetings = Enumerable.Range(0, 3)
            .Select(i => CreateAnalyzedMeeting(
                CreateDefaultAnalysisJson(),
                CreateDefaultReflectionJson(),
                DateTimeOffset.UtcNow.AddDays(-365 + i)))
            .ToArray();

        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(oldMeetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "all"));

        Assert.True(result.HasEnoughData);
        Assert.Equal(3, result.MeetingCount);
    }

    #endregion

    #region Open Trend

    [Fact]
    public async Task ExecuteAsync_ThreeMeetings_NoOpenTrend()
    {
        // Fewer than 4 meetings → null trend
        var meetings = CreateMeetingsWithReflections(3);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.Null(result.OpenTrend);
    }

    [Fact]
    public async Task ExecuteAsync_FourOrMoreMeetings_ReturnsOpenTrend()
    {
        var meetings = CreateMeetingsWithReflections(6);
        _meetingRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(meetings);

        var result = await _sut.ExecuteAsync(new GetJohariWindow.Query(UserId, "90d"));

        Assert.NotNull(result.OpenTrend);
    }

    #endregion

    #region Valid Ranges

    [Fact]
    public void ValidRanges_ContainsExpectedValues()
    {
        Assert.Contains("7d", GetJohariWindow.ValidRanges);
        Assert.Contains("30d", GetJohariWindow.ValidRanges);
        Assert.Contains("90d", GetJohariWindow.ValidRanges);
        Assert.Contains("all", GetJohariWindow.ValidRanges);
        Assert.Equal(4, GetJohariWindow.ValidRanges.Length);
    }

    #endregion

    #region Helpers

    private static Meeting[] CreateMeetingsWithReflections(int count, DateTimeOffset? baseDate = null)
    {
        var start = baseDate ?? DateTimeOffset.UtcNow.AddDays(-30);
        return Enumerable.Range(0, count)
            .Select(i => CreateAnalyzedMeeting(
                CreateDefaultAnalysisJson(),
                CreateDefaultReflectionJson(),
                start.AddDays(i)))
            .ToArray();
    }

    private static Meeting[] CreateAlignedMeetings(int count)
    {
        // Target participant will be "Alice" (highest avg talk time at 60%)
        // Self-assessment matches AI for Alice: ~60% talk time, high engagement, collaborative (≥0.6), no interruptions
        return Enumerable.Range(0, count)
            .Select(i =>
            {
                var analysis = CreateAnalysis(
                    talkTime: [("Alice", 60), ("Bob", 40)],
                    sentiments: [("Alice", "positive", 0.8), ("Bob", "neutral", 0.5)],
                    engagements: [("Alice", "high"), ("Bob", "medium")],
                    interruptions: []);

                var reflection = new ReflectionDto(
                    SelfAssessedTalkTime: 60,                   // Matches 60%
                    SelfAssessedEngagement: "Highly Engaged",   // Matches "high"
                    SelfAssessedTone: "Collaborative",           // Matches sentiment 0.8
                    InterruptionAwareness: "No",                 // Matches 0 interruptions
                    FreeformReflection: null,
                    PromptResponses: []);

                return CreateAnalyzedMeeting(
                    Serialize(analysis),
                    Serialize(reflection),
                    DateTimeOffset.UtcNow.AddDays(-20 + i));
            })
            .ToArray();
    }

    private static Meeting[] CreateMisalignedMeetings(int count)
    {
        // Target participant is "Alice" (highest avg talk time at 70%)
        // Self-assessment way off from AI for Alice
        return Enumerable.Range(0, count)
            .Select(i =>
            {
                var analysis = CreateAnalysis(
                    talkTime: [("Alice", 70), ("Bob", 30)],
                    sentiments: [("Alice", "negative", 0.2), ("Bob", "positive", 0.8)],
                    engagements: [("Alice", "low"), ("Bob", "high")],
                    interruptions: [("Alice", "Bob", 5)]);

                var reflection = new ReflectionDto(
                    SelfAssessedTalkTime: 30,                   // Way off from 70% (diff=40)
                    SelfAssessedEngagement: "Highly Engaged",   // AI says "low"
                    SelfAssessedTone: "Collaborative",           // AI says 0.2 sentiment
                    InterruptionAwareness: "No",                 // AI detected 5
                    FreeformReflection: null,
                    PromptResponses: []);

                return CreateAnalyzedMeeting(
                    Serialize(analysis),
                    Serialize(reflection),
                    DateTimeOffset.UtcNow.AddDays(-20 + i));
            })
            .ToArray();
    }

    private static Meeting CreateAnalyzedMeeting(
        string behavioralAnalysisJson,
        string? reflectionJson,
        DateTimeOffset? date = null)
    {
        var meeting = Meeting.Create(UserId, "Test Meeting", date ?? DateTimeOffset.UtcNow.AddDays(-1));
        meeting.SubmitTranscript("Test transcript content for analysis.");
        meeting.StartAnalysis();
        meeting.CompleteAnalysis("Test summary", "[]", "[]", behavioralAnalysisJson);

        if (reflectionJson is not null)
        {
            meeting.SubmitReflection(reflectionJson);
        }

        return meeting;
    }

    private static string CreateDefaultAnalysisJson()
    {
        var analysis = CreateAnalysis(
            talkTime: [("Alice", 45), ("Bob", 55)],
            sentiments: [("Alice", "positive", 0.7), ("Bob", "neutral", 0.5)],
            engagements: [("Alice", "high"), ("Bob", "medium")],
            interruptions: [("Alice", "Bob", 1)]);
        return Serialize(analysis);
    }

    private static string CreateDefaultReflectionJson()
    {
        var reflection = new ReflectionDto(
            SelfAssessedTalkTime: 45,
            SelfAssessedEngagement: "Highly Engaged",
            SelfAssessedTone: "Collaborative",
            InterruptionAwareness: "Yes",
            FreeformReflection: "This is a detailed reflection about my meeting behavior that exceeds the minimum length.",
            PromptResponses: []);
        return Serialize(reflection);
    }

    private static string Serialize<T>(T data) =>
        JsonSerializer.Serialize(data, JsonOptions);

    private static BehavioralAnalysisData CreateAnalysis(
        (string name, double pct)[]? talkTime = null,
        (string interrupter, string interrupted, int count)[]? interruptions = null,
        (string participant, string sentiment, double score)[]? sentiments = null,
        (string participant, string level)[]? engagements = null)
    {
        var talkTimeList = talkTime?
            .Select(t => new ParticipantTalkTime(t.name, t.pct, $"{t.pct}%"))
            .ToList() ?? [new ParticipantTalkTime("User", 50, "50%")];

        var interruptionPatterns = interruptions?
            .Select(i => new InterruptionPattern(i.interrupter, i.interrupted, i.count))
            .ToList() ?? [];

        var participantSentiments = sentiments?
            .Select(s => new ParticipantSentiment(s.participant, s.sentiment, s.score))
            .ToList() ?? [new ParticipantSentiment("User", "neutral", 0.5)];

        var engagementLevels = engagements?
            .Select(e => new ParticipantEngagement(e.participant, e.level, []))
            .ToList() ?? [new ParticipantEngagement("User", "medium", [])];

        return new BehavioralAnalysisData(
            new SpeakingDynamics(talkTimeList, interruptionPatterns, new Dictionary<string, double>()),
            new SentimentTone(participantSentiments, [], []),
            new CommunicationPatterns(0.8, [], engagementLevels),
            []);
    }

    #endregion
}
