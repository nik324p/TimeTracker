namespace TimeTracker.Core;

/// <summary>
/// Computes per-worker statistics (required / worked / under-worked hours; late and left-early
/// counts with and without reason) by summing the per-day <see cref="DayEvaluation"/>s over a
/// period window — the same derivation history uses, so the two never disagree.
/// </summary>
public sealed class StatisticsService(
    IUserRepository users,
    IWorkDayRepository workDays,
    IWorkScheduleRepository schedules,
    IScheduleExclusionRepository exclusions,
    TimeProvider clock,
    LatenessOptions latenessOptions,
    HistoryOptions historyOptions) : IStatisticsService
{
    public async Task<UserStatistics> GetByUserAsync(
        long userId, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default)
    {
        await users.EnsureExistsAsync(userId, ct);

        var schedule = await schedules.FindByUserAsync(userId, ct)
            ?? throw new ScheduleNotFoundException(userId);

        return await ComputeAsync(userId, schedule, period, ct);
    }

    public async Task<IReadOnlyList<UserStatistics>> GetSummaryAsync(
        int limit, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default)
    {
        if (limit < 1)
        {
            throw new InvalidLimitException(limit);
        }

        var effectiveLimit = Math.Min(limit, historyOptions.MaxLimit);
        var userIds = await users.ListUserIdsAsync(effectiveLimit, ct);

        var results = new List<UserStatistics>(userIds.Count);
        foreach (var userId in userIds)
        {
            // Skip users without a schedule rather than throw inside the loop (no "required" baseline).
            var schedule = await schedules.FindByUserAsync(userId, ct);
            if (schedule is null)
            {
                continue;
            }

            results.Add(await ComputeAsync(userId, schedule, period, ct));
        }

        return results;
    }

    private async Task<UserStatistics> ComputeAsync(
        long userId, WorkSchedule schedule, StatisticsPeriod period, CancellationToken ct)
    {
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

        // Enumerate every calendar day in the window so a no-show still counts its required hours.
        var start = from ?? EarliestActivity(days, userExclusions, today);
        var byDate = days
            .GroupBy(d => d.Date)
            .ToDictionary(g => g.Key, g => g.First());

        var required = TimeSpan.Zero;
        var worked = TimeSpan.Zero;
        var underWorked = TimeSpan.Zero;
        var lateWithReason = 0;
        var lateWithoutReason = 0;
        var earlyWithReason = 0;
        var earlyWithoutReason = 0;

        for (var date = start; date <= today; date = date.AddDays(1))
        {
            var fullyExcused = userExclusions.Any(e => e.Type == ExclusionType.FullWorkingDay && e.Covers(date));
            var dayExpected = schedule.IsWorkingDay(date) && !fullyExcused
                ? schedule.ExpectedDuration
                : TimeSpan.Zero;
            required += dayExpected;

            if (byDate.TryGetValue(date, out var day))
            {
                var eval = AttendanceEvaluator.Evaluate(day, schedule, userExclusions, latenessOptions);
                worked += eval.WorkedDuration;
                underWorked += eval.UnderWorked;

                if (eval.WasLate)
                {
                    if (eval.LateExcused)
                    {
                        lateWithReason++;
                    }
                    else
                    {
                        lateWithoutReason++;
                    }
                }

                if (eval.LeftEarly)
                {
                    if (eval.LeftEarlyExcused)
                    {
                        earlyWithReason++;
                    }
                    else
                    {
                        earlyWithoutReason++;
                    }
                }
            }
            else
            {
                // Scheduled working day with no tap: under-worked by the full expected duration.
                underWorked += dayExpected;
            }
        }

        return new UserStatistics(
            userId, period, from, today,
            required, worked, underWorked,
            lateWithoutReason, lateWithReason, earlyWithoutReason, earlyWithReason);
    }

    /// <summary>Earliest day with activity (for the entire-period window), or today if none.</summary>
    private static DateOnly EarliestActivity(
        IReadOnlyList<WorkDay> days, IReadOnlyList<ScheduleExclusion> userExclusions, DateOnly today)
    {
        var earliestDay = days.Count > 0 ? days.Min(d => d.Date) : today;
        var earliestExclusion = userExclusions.Count > 0
            ? userExclusions.Min(e => DateOnly.FromDateTime(e.StartDateTime.UtcDateTime))
            : today;
        return earliestDay < earliestExclusion ? earliestDay : earliestExclusion;
    }

    private static DateTimeOffset ToInstant(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
