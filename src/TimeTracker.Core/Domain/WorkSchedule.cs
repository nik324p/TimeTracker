namespace TimeTracker.Core;

/// <summary>
/// A worker's expected work time. One per user (upserted by <c>/work_time/set</c>). When
/// <see cref="FreeSchedule"/> is true, the worker has flexible hours: lateness and early-departure
/// are never flagged, though the expected hours still count toward "required to work".
/// </summary>
public sealed record WorkSchedule
{
    public long UserId { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public WorkDays Days { get; private set; }

    public bool FreeSchedule { get; private set; }

    private WorkSchedule()
    {
    }

    /// <summary>Creates a schedule, enforcing the schedule invariants.</summary>
    public static WorkSchedule Create(long userId, TimeOnly startTime, TimeOnly endTime, WorkDays days, bool freeSchedule)
    {
        if (userId <= 0)
        {
            throw new InvalidScheduleException("User id must be positive.");
        }

        var schedule = new WorkSchedule { UserId = userId };
        schedule.Apply(startTime, endTime, days, freeSchedule);
        return schedule;
    }

    /// <summary>Updates the schedule in place, re-enforcing the invariants.</summary>
    public void Update(TimeOnly startTime, TimeOnly endTime, WorkDays days, bool freeSchedule)
        => Apply(startTime, endTime, days, freeSchedule);

    private void Apply(TimeOnly startTime, TimeOnly endTime, WorkDays days, bool freeSchedule)
    {
        if (endTime <= startTime)
        {
            throw new InvalidScheduleException("End time must be after start time (overnight schedules are not supported).");
        }

        if (days == WorkDays.None)
        {
            throw new InvalidScheduleException("A schedule must include at least one working day.");
        }

        StartTime = startTime;
        EndTime = endTime;
        Days = days;
        FreeSchedule = freeSchedule;
    }

    /// <summary>True when <paramref name="date"/> falls on a scheduled working day.</summary>
    public bool IsWorkingDay(DateOnly date) => Days.Contains(date);

    /// <summary>Expected work duration for a single working day, before exclusions.</summary>
    public TimeSpan ExpectedDuration => EndTime - StartTime;
}
