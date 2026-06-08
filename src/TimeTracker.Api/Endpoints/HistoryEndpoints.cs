using FastEndpoints;
using TimeTracker.Core;

namespace TimeTracker.Api;

/// <summary>POST /work_time/history_by_user — a worker's attendance history (default period = month).</summary>
public sealed class HistoryByUserEndpoint(IHistoryService history) : Endpoint<UserHistoryRequest, HistoryResponse>
{
    public override void Configure()
    {
        Post("/work_time/history_by_user");
        AllowAnonymous();
        Description(b => b
            .Produces<HistoryResponse>(200)
            .ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "Get a worker's attendance history for the selected period (default: month).");
    }

    public override async Task HandleAsync(UserHistoryRequest req, CancellationToken ct)
    {
        var entries = await history.GetByUserAsync(req.UserId, req.Filter, ct);
        await Send.OkAsync(new HistoryResponse(entries.Select(e => e.ToResponse()).ToArray()), ct);
    }
}

/// <summary>POST /work_time/history — most-recent attendance across all workers (default period = month).</summary>
public sealed class HistoryEndpoint(IHistoryService history) : Endpoint<HistorySummaryRequest, HistoryResponse>
{
    public override void Configure()
    {
        Post("/work_time/history");
        AllowAnonymous();
        Description(b => b
            .Produces<HistoryResponse>(200)
            .ProducesProblem(400));
        Summary(s => s.Summary = "Get the most recent attendance history across all workers for the selected period (default: month).");
    }

    public override async Task HandleAsync(HistorySummaryRequest req, CancellationToken ct)
    {
        var entries = await history.GetAllAsync(req.Limit, req.Filter, ct);
        await Send.OkAsync(new HistoryResponse(entries.Select(e => e.ToResponse()).ToArray()), ct);
    }
}
