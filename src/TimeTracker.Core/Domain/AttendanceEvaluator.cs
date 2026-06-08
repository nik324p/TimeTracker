namespace TimeTracker.Core;

/// <summary>
/// The per-day evaluation of a work day against its schedule, grace, and exclusions. This single
/// result powers both history and statistics, so the two can never disagree.
/// </summary>
public sealed record DayEvaluation(
    DateOnly Date,
    bool IsWorkingDay,
    TimeSpan ExpectedDuration,
    TimeSpan WorkedDuration,
    bool WasLate,
    bool LateExcused,
    bool LeftEarly,
    bool LeftEarlyExcused,
    TimeSpan UnderWorked);

/// <summary>
/// Pure, side-effect-free derivation of lateness / early-departure / under-work for a single day.
/// No I/O and no clock — the taps already carry their timestamps — so it is fully unit-testable.
/// </summary>
public static class AttendanceEvaluator
{
    public static DayEvaluation Evaluate(
        WorkDay day,
        WorkSchedule schedule,
        IReadOnlyCollection<ScheduleExclusion> exclusions,
        LatenessOptions options)
    {
        ArgumentNullException.ThrowIfNull(day);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(exclusions);
        ArgumentNullException.ThrowIfNull(options);

        var worked = day.WorkedDuration ?? TimeSpan.Zero;
        var isWorkingDay = schedule.IsWorkingDay(day.Date);
        var fullyExcused = exclusions.Any(e => e.Type == ExclusionType.FullWorkingDay && e.Covers(day.Date));

        // Rules 2 & 3: an excused full day or a non-working day owes nothing and carries no flags;
        // worked time is still credited. (FullWorkingDay wins even on a scheduled working day.)
        if (fullyExcused || !isWorkingDay)
        {
            return new DayEvaluation(day.Date, isWorkingDay, TimeSpan.Zero, worked,
                WasLate: false, LateExcused: false, LeftEarly: false, LeftEarlyExcused: false, UnderWorked: TimeSpan.Zero);
        }

        // 4. A normal working day.
        var expected = schedule.ExpectedDuration;
        var underWorked = worked < expected ? expected - worked : TimeSpan.Zero;

        var wasLate = false;
        var lateExcused = false;
        var leftEarly = false;
        var leftEarlyExcused = false;

        if (!schedule.FreeSchedule)
        {
            if (day.ArrivalAt is { } arrival)
            {
                wasLate = arrival.TimeOfDay > schedule.StartTime.ToTimeSpan() + options.Grace;
                lateExcused = wasLate
                    && exclusions.Any(e => e.Type == ExclusionType.ArriveLater && e.AppliesTo(arrival));
            }

            if (day.DepartureAt is { } departure)
            {
                leftEarly = departure.TimeOfDay < schedule.EndTime.ToTimeSpan() - options.Grace;
                leftEarlyExcused = leftEarly
                    && exclusions.Any(e => e.Type == ExclusionType.LeaveEarlier && e.AppliesTo(departure));
            }
        }

        return new DayEvaluation(day.Date, IsWorkingDay: true, expected, worked,
            wasLate, lateExcused, leftEarly, leftEarlyExcused, underWorked);
    }
}
