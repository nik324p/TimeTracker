using System.ComponentModel.DataAnnotations;
using TimeTracker.Core;

namespace TimeTracker.Api;

public sealed record SetWorkTimeRequest(
    [property: Range(1, long.MaxValue)] long UserId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    [property: Required, MinLength(1)] IReadOnlyList<DayOfWeek> Days,
    bool FreeSchedule);

public sealed record AddExclusionRequest(
    [property: Range(1, long.MaxValue)] long UserId,
    ExclusionType TypeExclusion,
    DateTimeOffset StartDatetime,
    DateTimeOffset EndDatetime);

public sealed record WorkTimeResponse(
    long UserId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyList<DayOfWeek> Days,
    bool FreeSchedule);

public sealed record ExclusionResponse(
    long UserId,
    ExclusionType TypeExclusion,
    DateTimeOffset StartDatetime,
    DateTimeOffset EndDatetime);

public sealed record ExclusionsResponse(long UserId, IReadOnlyList<ExclusionResponse> Exclusions);

internal static partial class ResponseMappings
{
    public static WorkTimeResponse ToResponse(this WorkSchedule s) =>
        new(s.UserId, s.StartTime, s.EndTime, s.Days.ToDayList(), s.FreeSchedule);

    public static ExclusionResponse ToResponse(this ScheduleExclusion e) =>
        new(e.UserId, e.Type, e.StartDateTime, e.EndDateTime);
}
