namespace TimeTracker.Core;

/// <summary>
/// An NFC card bound to exactly one user. The (normalized) <see cref="Uid"/> is the identity and is
/// unique across the system. To move a card to another user, delete it then assign anew.
/// </summary>
public sealed record Card
{
    public string Uid { get; private set; } = default!; // CardUid, normalized

    public long UserId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    private Card()
    {
    }

    /// <summary>Binds a card UID to a user. Normalizes the UID; requires a positive user id.</summary>
    public static Card Assign(string uid, long userId, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var normalized = CardUidNormalizer.Normalize(uid);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new InvalidTapException("Card UID must not be empty.");
        }

        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "User id must be positive.");
        }

        return new Card
        {
            Uid = normalized,
            UserId = userId,
            AssignedAt = clock.GetUtcNow(),
        };
    }
}
