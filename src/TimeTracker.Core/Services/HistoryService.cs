namespace TimeTracker.Core;

/// <summary>
/// Projects stored work days into history entries with derived lateness/early flags
/// (via <see cref="AttendanceEvaluator"/>), for one user or across all users.
/// </summary>
public sealed class HistoryService(
    IUserRepository users,
    IWorkDayRepository workDays,
    IWorkScheduleRepository schedules,
    IScheduleExclusionRepository exclusions,
    TimeProvider clock,
    LatenessOptions latenessOptions,
    HistoryOptions historyOptions) : IHistoryService
{
    public async Task<IReadOnlyList<WorkDayHistoryEntry>> GetByUserAsync(
        long userId, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default)
    {
        await users.EnsureExistsAsync(userId, ct);

        var schedule = await schedules.FindByUserAsync(userId, ct);

        // Narrow days AND exclusions to the same window (as StatisticsService does), so the two
        // services can never disagree. EntirePeriod (from == null) keeps every recorded day.
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var from = period.ResolveFrom(today);

        IReadOnlyList<WorkDay> days;
        IReadOnlyList<ScheduleExclusion> userExclusions;
        if (from is { } lower)
        {
            days = await workDays.ListByUserInRangeAsync(userId, lower, today, ct);
            userExclusions = await exclusions.ListByUserInRangeAsync(
                userId, ToInstant(lower, TimeOnly.MinValue), ToInstant(today, TimeOnly.MaxValue), ct);
        }
        else
        {
            days = await workDays.ListByUserAsync(userId, ct);
            userExclusions = await exclusions.ListByUserAsync(userId, ct);
        }

        return days
            .OrderByDescending(d => d.Date)
            .Select(d => Project(d, schedule, userExclusions))
            .ToArray();
    }

    public async Task<IReadOnlyList<WorkDayHistoryEntry>> GetAllAsync(
        int limit, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default)
    {
        if (limit < 1)
        {
            throw new InvalidLimitException(limit);
        }

        var effectiveLimit = Math.Min(limit, historyOptions.MaxLimit);

        // Newest-first within the period window, capped at the limit; EntirePeriod keeps the plain cap.
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var from = period.ResolveFrom(today);
        var days = from is { } lower
            ? await workDays.ListAllInRangeAsync(lower, today, effectiveLimit, ct)
            : await workDays.ListAllAsync(effectiveLimit, ct);

        // Fetch each user's schedule + exclusions once, then evaluate every day.
        var scheduleCache = new Dictionary<long, WorkSchedule?>();
        var exclusionCache = new Dictionary<long, IReadOnlyList<ScheduleExclusion>>();
        var entries = new List<WorkDayHistoryEntry>(days.Count);

        foreach (var day in days)
        {
            if (!scheduleCache.TryGetValue(day.UserId, out var schedule))
            {
                schedule = await schedules.FindByUserAsync(day.UserId, ct);
                scheduleCache[day.UserId] = schedule;
            }

            if (!exclusionCache.TryGetValue(day.UserId, out var userExclusions))
            {
                // Window exclusions to match the days' window, so an out-of-window exclusion can
                // never leak into an in-window day's evaluation (keeps parity with statistics).
                userExclusions = from is null
                    ? await exclusions.ListByUserAsync(day.UserId, ct)
                    : await exclusions.ListByUserInRangeAsync(
                        day.UserId, ToInstant(from.Value, TimeOnly.MinValue), ToInstant(today, TimeOnly.MaxValue), ct);
                exclusionCache[day.UserId] = userExclusions;
            }

            entries.Add(Project(day, schedule, userExclusions));
        }

        return entries;
    }

    /// <summary>
    /// Builds a history entry. With no schedule, lateness/early cannot be derived, so those flags
    /// are false and only the raw taps + worked duration are reported.
    /// </summary>
    private WorkDayHistoryEntry Project(
        WorkDay day, WorkSchedule? schedule, IReadOnlyList<ScheduleExclusion> userExclusions)
    {
        if (schedule is null)
        {
            return new WorkDayHistoryEntry(
                day.UserId, day.Date, day.ArrivalAt, day.DepartureAt, day.WorkedDuration,
                WasLate: false, LateExcused: false, LeftEarly: false, LeftEarlyExcused: false);
        }

        var eval = AttendanceEvaluator.Evaluate(day, schedule, userExclusions, latenessOptions);
        return new WorkDayHistoryEntry(
            day.UserId, day.Date, day.ArrivalAt, day.DepartureAt, day.WorkedDuration,
            eval.WasLate, eval.LateExcused, eval.LeftEarly, eval.LeftEarlyExcused);
    }

    private static DateTimeOffset ToInstant(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
