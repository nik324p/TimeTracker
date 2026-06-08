namespace TimeTracker.Tests;

/// <summary>
/// FakeTimeProvider factories for deterministic time. Default instant is Thursday 2026-06-04 09:00
/// UTC — a scheduled working day at the default schedule start. FakeTimeProvider's local zone is UTC,
/// so GetLocalNow() == GetUtcNow() and calendar-day bucketing is deterministic.
/// </summary>
public static class TestClock
{
    public static readonly DateOnly DefaultDate = new(2026, 6, 4); // Thursday
    public static readonly DateTimeOffset NineAm = new(2026, 6, 4, 9, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider At(DateTimeOffset instant) => new(instant);

    public static FakeTimeProvider AtNineAm() => new(NineAm);

    /// <summary>A UTC instant on <see cref="DefaultDate"/> at the given time of day.</summary>
    public static DateTimeOffset On(TimeOnly time) => new(DefaultDate.ToDateTime(time), TimeSpan.Zero);

    /// <summary>A UTC instant on a specific date at the given time of day.</summary>
    public static DateTimeOffset On(DateOnly date, TimeOnly time) => new(date.ToDateTime(time), TimeSpan.Zero);
}
