using Microsoft.EntityFrameworkCore;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class WorkScheduleRepository(AppDbContext db) : IWorkScheduleRepository
{
    // Tracked: the set/upsert path mutates the returned schedule via Update() before SaveChanges.
    public Task<WorkSchedule?> FindByUserAsync(long userId, CancellationToken ct = default) =>
        db.WorkSchedules.FirstOrDefaultAsync(w => w.UserId == userId, ct);

    public void Add(WorkSchedule schedule) => db.WorkSchedules.Add(schedule);
}
