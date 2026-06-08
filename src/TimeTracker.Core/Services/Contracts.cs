namespace TimeTracker.Core;

/// <summary>Result of a card tap: the card, its owner, and which tap was recorded.</summary>
public sealed record TapResult(string CardUid, long UserId, TapKind Kind);

/// <summary>A card-to-user binding (assign/delete responses).</summary>
public sealed record CardAssignment(string CardUid, long UserId);

/// <summary>Command to set (upsert) a worker's schedule.</summary>
public sealed record SetScheduleCommand(
    long UserId, TimeOnly StartTime, TimeOnly EndTime, WorkDays Days, bool FreeSchedule);

/// <summary>Command to add a schedule exclusion.</summary>
public sealed record AddExclusionCommand(
    long UserId, ExclusionType Type, DateTimeOffset StartDateTime, DateTimeOffset EndDateTime);

/// <summary>One day of a worker's attendance history, with derived lateness/early flags.</summary>
public sealed record WorkDayHistoryEntry(
    long UserId,
    DateOnly Date,
    DateTimeOffset? ArrivalAt,
    DateTimeOffset? DepartureAt,
    TimeSpan? WorkedDuration,
    bool WasLate,
    bool LateExcused,
    bool LeftEarly,
    bool LeftEarlyExcused);

/// <summary>Aggregated statistics for one worker over a period.</summary>
public sealed record UserStatistics(
    long UserId,
    StatisticsPeriod Period,
    DateOnly? FromDate, // null when EntirePeriod
    DateOnly ToDate,
    TimeSpan RequiredToWork,
    TimeSpan Worked,
    TimeSpan UnderWorked,
    int TimesLateWithoutReason,
    int TimesLateWithReason,
    int TimesLeftEarlyWithoutReason,
    int TimesLeftEarlyWithReason);
