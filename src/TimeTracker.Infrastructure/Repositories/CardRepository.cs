using Microsoft.EntityFrameworkCore;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class CardRepository(AppDbContext db) : ICardRepository
{
    // Tracked: the delete path loads via this method then Remove()s the entity.
    public Task<Card?> FindByUidAsync(string cardUid, CancellationToken ct = default) =>
        db.Cards.FirstOrDefaultAsync(c => c.Uid == cardUid, ct);

    public async Task<IReadOnlyList<Card>> ListByUserAsync(long userId, CancellationToken ct = default) =>
        await db.Cards.AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

    public void Add(Card card) => db.Cards.Add(card);

    public void Remove(Card card) => db.Cards.Remove(card);

    public void RemoveRange(IEnumerable<Card> cards) => db.Cards.RemoveRange(cards);
}
