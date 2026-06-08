namespace TimeTracker.Core;

/// <summary>Guard helpers shared by the application services.</summary>
internal static class UserRepositoryExtensions
{
    /// <summary>Throws <see cref="UserNotFoundException"/> when no user has the given id.</summary>
    public static async Task EnsureExistsAsync(this IUserRepository users, long userId, CancellationToken ct)
    {
        if (!await users.ExistsAsync(userId, ct))
        {
            throw new UserNotFoundException(userId);
        }
    }
}
