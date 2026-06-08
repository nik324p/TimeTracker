using FastEndpoints;
using TimeTracker.Core;

namespace TimeTracker.Api;

/// <summary>POST /work_time/statistics_by_user — one worker's statistics (default period = month).</summary>
public sealed class StatisticsByUserEndpoint(IStatisticsService stats)
    : Endpoint<UserStatisticsRequest, UserStatisticsResponse>
{
    public override void Configure()
    {
        Post("/work_time/statistics_by_user");
        AllowAnonymous();
        Description(b => b
            .Produces<UserStatisticsResponse>(200)
            .ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "Get a worker's statistics for the selected period (default: month).");
    }

    public override async Task HandleAsync(UserStatisticsRequest req, CancellationToken ct)
    {
        var statistics = await stats.GetByUserAsync(req.UserId, req.Filter, ct);
        await Send.OkAsync(statistics.ToResponse(), ct);
    }
}

/// <summary>POST /work_time/statistics — summary statistics across workers (default period = month).</summary>
public sealed class StatisticsEndpoint(IStatisticsService stats)
    : Endpoint<StatisticsSummaryRequest, StatisticsSummaryResponse>
{
    public override void Configure()
    {
        Post("/work_time/statistics");
        AllowAnonymous();
        Description(b => b
            .Produces<StatisticsSummaryResponse>(200)
            .ProducesProblem(400));
        Summary(s => s.Summary = "Get summary statistics across workers for the selected period (default: month).");
    }

    public override async Task HandleAsync(StatisticsSummaryRequest req, CancellationToken ct)
    {
        var perUserStats = await stats.GetSummaryAsync(req.Limit, req.Filter, ct);
        var perUser = perUserStats.Select(s => s.ToResponse()).ToArray();

        var summary = new StatisticsSummaryResponse(
            perUser.Sum(u => u.RequiredMinutes),
            perUser.Sum(u => u.WorkedMinutes),
            perUser.Sum(u => u.UnderworkedMinutes),
            perUser.Sum(u => u.LateWithoutReason),
            perUser.Sum(u => u.LateWithReason),
            perUser.Sum(u => u.LeftEarlyWithoutReason),
            perUser.Sum(u => u.LeftEarlyWithReason),
            perUser);

        await Send.OkAsync(summary, ct);
    }
}
