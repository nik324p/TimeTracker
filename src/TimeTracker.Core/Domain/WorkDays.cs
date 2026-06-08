namespace TimeTracker.Core;

/// <summary>
/// The set of working days of the week, as a bit flag. The Api maps the spec's <c>days</c> list
/// to/from this enum via <see cref="WorkDaysExtensions"/>.
/// </summary>
[Flags]
public enum WorkDays
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    All = Weekdays | Saturday | Sunday,
}
