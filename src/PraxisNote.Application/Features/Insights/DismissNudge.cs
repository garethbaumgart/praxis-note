using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.BlindSpotNudges;

namespace PraxisNote.Application.Features.Insights;

public sealed class DismissNudge(IBlindSpotNudgeRepository nudgeRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid NudgeId);

    public const string NotFoundError = "NUDGE_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var nudge = await nudgeRepository.GetByIdAsync(command.NudgeId, cancellationToken);
        if (nudge is null || nudge.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        nudge.Dismiss();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
