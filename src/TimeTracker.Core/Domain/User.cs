namespace TimeTracker.Core;

/// <summary>
/// A worker. Minimal in this spec — created explicitly (seeded) since there is no create-user
/// endpoint; card assignment / schedule set against an unknown id throw <see cref="UserNotFoundException"/>.
/// </summary>
public sealed record User
{
    public long Id { get; private set; } // UserId

    public string? DisplayName { get; private set; }

    private readonly List<Card> _cards = [];

    /// <summary>Cards bound to this user. Populated by the repository; not mutated through this nav.</summary>
    public IReadOnlyList<Card> Cards => _cards;

    private User()
    {
    }

    /// <summary>Creates a user. <paramref name="id"/> must be positive.</summary>
    public static User Create(long id, string? displayName = null)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "User id must be positive.");
        }

        return new User { Id = id, DisplayName = displayName };
    }
}
