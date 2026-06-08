namespace TimeTracker.Core;

/// <summary>
/// Records a card tap: identifies the worker by card, opens or completes today's
/// <see cref="WorkDay"/>, commits, then publishes a <see cref="CardTappedEvent"/>.
/// </summary>
public sealed class TapService(
    ICardRepository cards,
    IUserRepository users,
    IWorkDayRepository workDays,
    IUnitOfWork unitOfWork,
    IEventPublisher publisher,
    TimeProvider clock) : ITapService
{
    public async Task<TapResult> TouchAsync(string cardUid, CancellationToken ct = default)
    {
        var normalized = CardUidNormalizer.Normalize(cardUid);

        var card = await cards.FindByUidAsync(normalized, ct)
            ?? throw new CardNotFoundException(normalized);

        if (!await users.ExistsAsync(card.UserId, ct))
        {
            throw new UserNotFoundException(card.UserId);
        }

        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var now = clock.GetUtcNow();

        var workDay = await workDays.FindByUserAndDateAsync(card.UserId, today, ct);

        TapKind kind;
        if (workDay is null)
        {
            workDay = WorkDay.Open(card.UserId, today, now);
            workDays.Add(workDay);
            kind = TapKind.Arrival;
        }
        else
        {
            // Throws AlreadyTappedException on a third tap — the "one telling failure".
            kind = workDay.RecordTap(now);
        }

        await unitOfWork.SaveChangesAsync(ct);

        // Publish only after the commit succeeds; the publisher impl swallows broker failures.
        await publisher.PublishAsync(new CardTappedEvent(card.Uid, card.UserId, kind, now), ct);

        return new TapResult(card.Uid, card.UserId, kind);
    }
}
