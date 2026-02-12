using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.BlindSpotNudges;

namespace PraxisNote.Application.Features.Insights;

public sealed class GenerateBlindSpotNudges(
    IBlindSpotNudgeRepository nudgeRepository,
    GetJohariWindow getJohariWindow,
    IUnitOfWork unitOfWork)
{
    public record Query(Guid UserId, Guid ProfileId, string Range);

    public record BlindSpotNudgeDto(
        Guid Id,
        string Dimension,
        string Suggestion,
        string BlindSpotDescription,
        string Status);

    public async Task<IReadOnlyList<BlindSpotNudgeDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        // Check if user already has active nudges — if so, return them
        var existing = await nudgeRepository.GetActiveByUserAsync(query.UserId, query.ProfileId, cancellationToken);
        if (existing.Count > 0)
        {
            return existing.Select(ToDto).ToList();
        }

        // Get Johari Window data to find blind spots
        var johariQuery = new GetJohariWindow.Query(query.UserId, query.ProfileId, query.Range);
        var johariResult = await getJohariWindow.ExecuteAsync(johariQuery, cancellationToken);

        if (!johariResult.HasEnoughData || johariResult.BlindSpots.Count == 0)
        {
            return [];
        }

        // Generate nudges from blind spots (up to 3)
        var nudges = johariResult.BlindSpots
            .Take(3)
            .Select(bs => BlindSpotNudge.Create(
                query.UserId,
                query.ProfileId,
                bs.Dimension,
                GetSuggestion(bs.Dimension),
                bs.Description))
            .ToList();

        await nudgeRepository.AddRangeAsync(nudges, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return nudges.Select(ToDto).ToList();
    }

    private static BlindSpotNudgeDto ToDto(BlindSpotNudge nudge) => new(
        nudge.Id,
        nudge.Dimension,
        nudge.Suggestion,
        nudge.BlindSpotDescription,
        nudge.Status.ToString());

    private static string GetSuggestion(string dimension) => dimension switch
    {
        "Talk Time" => "Try the 60-second rule: after speaking for a minute, pause and invite others to share their perspective.",
        "Engagement" => "Try the active contribution rule: aim to add at least one question or building-on comment every 5 minutes.",
        "Tone" => "Try opening with agreement — start your next 3 responses with 'I agree that...' or 'Good point, and...'",
        "Interruptions" => "Try the 3-second pause: wait 3 full seconds after someone finishes speaking before you respond.",
        _ => "Review the AI feedback and reflect on one specific behavior to adjust in your next meeting."
    };
}
