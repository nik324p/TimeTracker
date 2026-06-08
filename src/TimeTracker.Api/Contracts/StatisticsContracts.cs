using System.ComponentModel.DataAnnotations;
using TimeTracker.Core;

namespace TimeTracker.Api;

// statistics (summary): limit + optional period filter (default month, like statistics_by_user).
public sealed record StatisticsSummaryRequest(
    [property: Range(1, 1000)] int Limit,
    StatisticsPeriod Filter = StatisticsPeriod.Month);

public sealed record UserStatisticsRequest(
    [property: Range(1, long.MaxValue)] long UserId,
    StatisticsPeriod Filter = StatisticsPeriod.Month);

public sealed record UserStatisticsResponse(
    long UserId,
    StatisticsPeriod Filter,
    DateOnly? FromDate,
    DateOnly ToDate,
    long RequiredMinutes,
    long WorkedMinutes,
    long UnderworkedMinutes,
    int LateWithoutReason,
    int LateWithReason,
    int LeftEarlyWithoutReason,
    int LeftEarlyWithReason);

public sealed record StatisticsSummaryResponse(
    long TotalRequiredMinutes,
    long TotalWorkedMinutes,
    long TotalUnderworkedMinutes,
    int TotalLateWithoutReason,
    int TotalLateWithReason,
    int TotalLeftEarlyWithoutReason,
    int TotalLeftEarlyWithReason,
    IReadOnlyList<UserStatisticsResponse> PerUser);

internal static partial class ResponseMappings
{
    public static UserStatisticsResponse ToResponse(this UserStatistics s) =>
        new(s.UserId, s.Period, s.FromDate, s.ToDate,
            s.RequiredToWork.ToMinutes(), s.Worked.ToMinutes(), s.UnderWorked.ToMinutes(),
            s.TimesLateWithoutReason, s.TimesLateWithReason,
            s.TimesLeftEarlyWithoutReason, s.TimesLeftEarlyWithReason);
}
