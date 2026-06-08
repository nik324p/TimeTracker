using System.ComponentModel.DataAnnotations;
using TimeTracker.Core;

namespace TimeTracker.Api;

// history (this user): user_id + optional period filter (default month).
public sealed record UserHistoryRequest(
    [property: Range(1, long.MaxValue)] long UserId,
    StatisticsPeriod Filter = StatisticsPeriod.Month);

// history (all workers): limit + optional period filter (default month).
public sealed record HistorySummaryRequest(
    [property: Range(1, 1000)] int Limit,
    StatisticsPeriod Filter = StatisticsPeriod.Month);

public sealed record HistoryEntryResponse(
    long UserId,
    DateOnly Date,
    DateTimeOffset? Arrival,
    DateTimeOffset? Departure,
    long? WorkedMinutes,
    bool WasLate,
    bool LateExcused,
    bool LeftEarly,
    bool LeftEarlyExcused);

public sealed record HistoryResponse(IReadOnlyList<HistoryEntryResponse> Entries);

internal static partial class ResponseMappings
{
    public static HistoryEntryResponse ToResponse(this WorkDayHistoryEntry e) =>
        new(e.UserId, e.Date, e.ArrivalAt, e.DepartureAt, e.WorkedDuration?.ToMinutes(),
            e.WasLate, e.LateExcused, e.LeftEarly, e.LeftEarlyExcused);
}
