using System.Text.Json.Serialization;

namespace TimeTracker.Core;

/// <summary>Which tap of the day was recorded. First tap = arrival, second = departure.</summary>
public enum TapKind
{
    Arrival = 0,
    Departure = 1,
}

/// <summary>
/// A sanctioned deviation from the schedule. Wire labels: "arrive later", "leave earlier",
/// "full working day" — emitted via <see cref="JsonStringEnumMemberNameAttribute"/> (the snake_case
/// policy can't produce spaces). The attribute is wire-format metadata only; Core stays serializer-free.
/// </summary>
public enum ExclusionType
{
    /// <summary>Late arrival is permitted (counts as late WITH reason) for covered days.</summary>
    [JsonStringEnumMemberName("arrive later")] ArriveLater = 0,

    /// <summary>Early departure is permitted (counts as left-early WITH reason) for covered days.</summary>
    [JsonStringEnumMemberName("leave earlier")] LeaveEarlier = 1,

    /// <summary>Vacation / day-off: that day owes 0 expected hours and is never late/early.</summary>
    [JsonStringEnumMemberName("full working day")] FullWorkingDay = 2,
}

/// <summary>Statistics aggregation window. Default is <see cref="Month"/>.</summary>
public enum StatisticsPeriod
{
    Week = 0,
    Month = 1,
    Year = 2,
    [JsonStringEnumMemberName("entire period")] EntirePeriod = 3,
}
