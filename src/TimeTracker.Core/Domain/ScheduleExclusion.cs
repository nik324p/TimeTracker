namespace TimeTracker.Core;

/// <summary>
/// A sanctioned deviation from the schedule over a datetime range — vacation/day-off
/// (<see cref="ExclusionType.FullWorkingDay"/>), permitted late arrival
/// (<see cref="ExclusionType.ArriveLater"/>), or permitted early departure
/// (<see cref="ExclusionType.LeaveEarlier"/>).
/// </summary>
public sealed record ScheduleExclusion
{
    public Guid Id { get; private set; }

    public long UserId { get; private set; }

    public ExclusionType Type { get; private set; }

    public DateTimeOffset StartDateTime { get; private set; }

    public DateTimeOffset EndDateTime { get; private set; }

    private ScheduleExclusion()
    {
    }

    /// <summary>Creates an exclusion, enforcing a positive user id and a non-inverted range.</summary>
    public static ScheduleExclusion Create(long userId, ExclusionType type, DateTimeOffset start, DateTimeOffset end)
    {
        if (userId <= 0)
        {
            throw new InvalidExclusionException("User id must be positive.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new InvalidExclusionException($"Unknown exclusion type '{type}'.");
        }

        if (end < start)
        {
            throw new InvalidExclusionException("End datetime must not be earlier than start datetime.");
        }

        return new ScheduleExclusion
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Type = type,
            StartDateTime = start,
            EndDateTime = end,
        };
    }

    /// <summary>True when the exclusion's [Start, End] range overlaps the given calendar day.</summary>
    public bool Covers(DateOnly date)
    {
        // Treat the stored instants as UTC (single-timezone assumption, overview.md §12) and compare
        // calendar days, so the result is deterministic and independent of the machine timezone.
        var startDate = DateOnly.FromDateTime(StartDateTime.UtcDateTime);
        var endDate = DateOnly.FromDateTime(EndDateTime.UtcDateTime);
        return startDate <= date && date <= endDate;
    }

    /// <summary>True when <paramref name="instant"/> falls within [Start, End].</summary>
    public bool AppliesTo(DateTimeOffset instant) => StartDateTime <= instant && instant <= EndDateTime;
}
