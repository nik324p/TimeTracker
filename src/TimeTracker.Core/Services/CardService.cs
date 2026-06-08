namespace TimeTracker.Core;

/// <summary>Card management: assign, delete, list, and bulk-delete a user's cards.</summary>
public sealed class CardService(
    IUserRepository users,
    ICardRepository cards,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICardService
{
    public async Task<CardAssignment> AssignAsync(long userId, string cardUid, CancellationToken ct = default)
    {
        await users.EnsureExistsAsync(userId, ct);

        var normalized = CardUidNormalizer.Normalize(cardUid);
        if (await cards.FindByUidAsync(normalized, ct) is not null)
        {
            throw new CardAlreadyAssignedException(normalized);
        }

        var card = Card.Assign(cardUid, userId, clock);
        cards.Add(card);
        await unitOfWork.SaveChangesAsync(ct);

        return new CardAssignment(card.Uid, card.UserId);
    }

    public async Task<CardAssignment> DeleteAsync(string cardUid, CancellationToken ct = default)
    {
        var normalized = CardUidNormalizer.Normalize(cardUid);
        var card = await cards.FindByUidAsync(normalized, ct)
            ?? throw new CardNotFoundException(normalized);

        cards.Remove(card);
        await unitOfWork.SaveChangesAsync(ct);

        return new CardAssignment(card.Uid, card.UserId);
    }

    public async Task<IReadOnlyList<string>> ListByUserAsync(long userId, CancellationToken ct = default)
    {
        await users.EnsureExistsAsync(userId, ct);

        var list = await cards.ListByUserAsync(userId, ct);
        return list.Select(c => c.Uid).ToArray();
    }

    public async Task<IReadOnlyList<string>> DeleteAllByUserAsync(long userId, CancellationToken ct = default)
    {
        await users.EnsureExistsAsync(userId, ct);

        var list = await cards.ListByUserAsync(userId, ct);
        var uids = list.Select(c => c.Uid).ToArray();

        if (list.Count > 0)
        {
            cards.RemoveRange(list);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return uids;
    }
}
