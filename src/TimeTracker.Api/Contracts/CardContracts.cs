using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Api;

// card_uid required, bounded.
public sealed record TouchCardRequest(
    [property: Required, MaxLength(64)] string CardUid);

public sealed record AssignCardRequest(
    [property: Range(1, long.MaxValue)] long UserId,
    [property: Required, MaxLength(64)] string CardUid);

public sealed record DeleteCardRequest(
    [property: Required, MaxLength(64)] string CardUid);

// { card_uid, user_id } — shared by touch / assign / delete.
public sealed record CardUserResponse(string CardUid, long UserId);

// { user_id, cards: [card_uid, ...] }
public sealed record UserCardsResponse(long UserId, IReadOnlyList<string> Cards);
