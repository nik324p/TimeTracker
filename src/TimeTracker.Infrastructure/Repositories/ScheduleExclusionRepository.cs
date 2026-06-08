using Microsoft.EntityFrameworkCore;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class ScheduleExclusionRepository(AppDbContext db) : IScheduleExclusionRepository
{
    public async Task<IReadOnlyList<ScheduleExclusion>> ListByUserAsync(long userId, CancellationToken ct = default) =>
        await db.ScheduleExclusions.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.StartDateTime)
            .ToListAsync(ct);

    // Overlap (not strict containment): an exclusion straddling a window edge still applies in-window.
    public async Task<IReadOnlyList<ScheduleExclusion>> ListByUserInRangeAsync(
        long userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) =>
        await db.ScheduleExclusions.AsNoTracking()
            .Where(e => e.UserId == userId && e.StartDateTime <= to && e.EndDateTime >= from)
            .OrderBy(e => e.StartDateTime)
            .ToListAsync(ct);

    public void Add(ScheduleExclusion exclusion) => db.ScheduleExclusions.Add(exclusion);
}
