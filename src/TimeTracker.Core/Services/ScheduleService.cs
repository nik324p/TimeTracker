namespace TimeTracker.Core;

/// <summary>Sets (upserts) and reads a worker's <see cref="WorkSchedule"/>.</summary>
public sealed class ScheduleService(
    IUserRepository users,
    IWorkScheduleRepository schedules,
    IUnitOfWork unitOfWork) : IScheduleService
{
    public async Task<WorkSchedule> SetAsync(SetScheduleCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await users.EnsureExistsAsync(command.UserId, ct);

        var existing = await schedules.FindByUserAsync(command.UserId, ct);
        WorkSchedule schedule;
        if (existing is null)
        {
            schedule = WorkSchedule.Create(
                command.UserId, command.StartTime, command.EndTime, command.Days, command.FreeSchedule);
            schedules.Add(schedule);
        }
        else
        {
            existing.Update(command.StartTime, command.EndTime, command.Days, command.FreeSchedule);
            schedule = existing;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task<WorkSchedule> GetAsync(long userId, CancellationToken ct = default)
    {
        await users.EnsureExistsAsync(userId, ct);

        return await schedules.FindByUserAsync(userId, ct)
            ?? throw new ScheduleNotFoundException(userId);
    }
}
