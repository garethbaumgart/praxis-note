namespace PraxisNote.Application.Features.Insights;

public record JohariWindowDto(
    int OpenPercentage,
    int BlindSpotPercentage,
    int HiddenPercentage,
    int UnknownPercentage,
    int MeetingCount,
    int MinimumMeetings,
    bool HasEnoughData,
    double? OpenTrend,
    List<JohariDimensionDto> Dimensions,
    List<BlindSpotDetailDto> BlindSpots);

public record JohariDimensionDto(
    string Name,
    string Quadrant,
    string SelfValue,
    string AiValue,
    string? Explanation);

public record BlindSpotDetailDto(
    string Dimension,
    string Description,
    int MeetingCount);
