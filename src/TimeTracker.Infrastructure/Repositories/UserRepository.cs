using Microsoft.EntityFrameworkCore;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindAsync(long userId, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

    public Task<bool> ExistsAsync(long userId, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Id == userId, ct);

    // No created-at column in this spec; approximate "newest-active first" by descending id.
    public async Task<IReadOnlyList<long>> ListUserIdsAsync(int limit, CancellationToken ct = default) =>
        await db.Users.AsNoTracking()
            .OrderByDescending(u => u.Id)
            .Take(limit)
            .Select(u => u.Id)
            .ToListAsync(ct);

    public void Add(User user) => db.Users.Add(user);
}
