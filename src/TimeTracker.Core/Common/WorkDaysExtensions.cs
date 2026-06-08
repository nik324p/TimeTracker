namespace TimeTracker.Core;

/// <summary>
/// Converts between the <see cref="WorkDays"/> flag enum and a list of <see cref="DayOfWeek"/>,
/// and tests membership for a given calendar date. Shared by Core, the Api (request mapping),
/// and Infrastructure (persistence).
/// </summary>
public static class WorkDaysExtensions
{
    /// <summary>Builds a <see cref="WorkDays"/> flag set from a sequence of <see cref="DayOfWeek"/>.</summary>
    public static WorkDays From(IEnumerable<DayOfWeek> days)
    {
        ArgumentNullException.ThrowIfNull(days);
        var result = WorkDays.None;
        foreach (var day in days)
        {
            result |= ToFlag(day);
        }

        return result;
    }

    /// <summary>Expands a <see cref="WorkDays"/> flag set into an ordered (Mon..Sun) list of days.</summary>
    public static IReadOnlyList<DayOfWeek> ToDayList(this WorkDays days)
    {
        // Iterate Monday..Sunday so the output order is stable and human-friendly.
        DayOfWeek[] order =
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
        ];

        return order.Where(d => days.HasFlag(ToFlag(d))).ToArray();
    }

    /// <summary>True when the flag set includes the day-of-week of <paramref name="date"/>.</summary>
    public static bool Contains(this WorkDays days, DateOnly date) => days.HasFlag(ToFlag(date.DayOfWeek));

    private static WorkDays ToFlag(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WorkDays.Monday,
        DayOfWeek.Tuesday => WorkDays.Tuesday,
        DayOfWeek.Wednesday => WorkDays.Wednesday,
        DayOfWeek.Thursday => WorkDays.Thursday,
        DayOfWeek.Friday => WorkDays.Friday,
        DayOfWeek.Saturday => WorkDays.Saturday,
        DayOfWeek.Sunday => WorkDays.Sunday,
        _ => WorkDays.None,
    };
}
