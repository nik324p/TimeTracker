namespace TimeTracker.Core;

/// <summary>
/// Shared resolution of a <see cref="StatisticsPeriod"/> into a date window, so history and statistics
/// agree on what "week"/"month"/"year" mean (and never disagree on which days a period covers).
/// </summary>
public static class StatisticsPeriodExtensions
{
    /// <summary>
    /// Inclusive lower bound of the period window relative to <paramref name="today"/>, or
    /// <c>null</c> for <see cref="StatisticsPeriod.EntirePeriod"/> (no lower bound).
    /// </summary>
    public static DateOnly? ResolveFrom(this StatisticsPeriod period, DateOnly today) => period switch
    {
        // ISO week: Monday of the week containing today.
        StatisticsPeriod.Week => today.AddDays(-(((int)today.DayOfWeek + 6) % 7)),
        StatisticsPeriod.Month => new DateOnly(today.Year, today.Month, 1),
        StatisticsPeriod.Year => new DateOnly(today.Year, 1, 1),
        StatisticsPeriod.EntirePeriod => null,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown statistics period."),
    };
}
