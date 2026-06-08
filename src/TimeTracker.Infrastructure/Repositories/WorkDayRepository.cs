using Microsoft.EntityFrameworkCore;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class WorkDayRepository(AppDbContext db) : IWorkDayRepository
{
    // Tracked: touch records the second tap on the returned WorkDay before SaveChanges.
    public Task<WorkDay?> FindByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct = default) =>
        db.WorkDays.FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, ct);

    public async Task<IReadOnlyList<WorkDay>> ListByUserAsync(long userId, CancellationToken ct = default) =>
        await db.WorkDays.AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.Date)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkDay>> ListByUserInRangeAsync(
        long userId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await db.WorkDays.AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkDay>> ListAllAsync(int limit, CancellationToken ct = default) =>
        await db.WorkDays.AsNoTracking()
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.ArrivalAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkDay>> ListAllInRangeAsync(
        DateOnly from, DateOnly to, int limit, CancellationToken ct = default) =>
        await db.WorkDays.AsNoTracking()
            .Where(d => d.Date >= from && d.Date <= to)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.ArrivalAt)
            .Take(limit)
            .ToListAsync(ct);

    public void Add(WorkDay workDay) => db.WorkDays.Add(workDay);
}
