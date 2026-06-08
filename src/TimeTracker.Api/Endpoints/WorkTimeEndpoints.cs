using FastEndpoints;
using TimeTracker.Core;

namespace TimeTracker.Api;

/// <summary>POST /work_time/set — set (upsert) a worker's schedule.</summary>
public sealed class SetWorkTimeEndpoint(IScheduleService schedules) : Endpoint<SetWorkTimeRequest, WorkTimeResponse>
{
    public override void Configure()
    {
        Post("/work_time/set");
        AllowAnonymous();
        Description(b => b
            .Produces<WorkTimeResponse>(200)
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(422));
        Summary(s => s.Summary = "Set the worker's work schedule.");
    }

    public override async Task HandleAsync(SetWorkTimeRequest req, CancellationToken ct)
    {
        var command = new SetScheduleCommand(
            req.UserId, req.StartTime, req.EndTime, WorkDaysExtensions.From(req.Days), req.FreeSchedule);
        var schedule = await schedules.SetAsync(command, ct);
        await Send.OkAsync(schedule.ToResponse(), ct);
    }
}

/// <summary>POST /work_time/get — read a worker's schedule.</summary>
public sealed class GetWorkTimeEndpoint(IScheduleService schedules) : Endpoint<UserIdRequest, WorkTimeResponse>
{
    public override void Configure()
    {
        Post("/work_time/get");
        AllowAnonymous();
        Description(b => b
            .Produces<WorkTimeResponse>(200)
            .ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "Get the worker's work schedule.");
    }

    public override async Task HandleAsync(UserIdRequest req, CancellationToken ct)
    {
        var schedule = await schedules.GetAsync(req.UserId, ct);
        await Send.OkAsync(schedule.ToResponse(), ct);
    }
}

/// <summary>POST /work_time/add_exclusion — add a schedule exclusion.</summary>
public sealed class AddExclusionEndpoint(IExclusionService exclusions) : Endpoint<AddExclusionRequest, ExclusionResponse>
{
    public override void Configure()
    {
        Post("/work_time/add_exclusion");
        AllowAnonymous();
        Description(b => b
            .Produces<ExclusionResponse>(200)
            .ProducesProblem(400).ProducesProblem(404).ProducesProblem(422));
        Summary(s => s.Summary = "Add an exclusion to the work schedule.");
    }

    public override async Task HandleAsync(AddExclusionRequest req, CancellationToken ct)
    {
        var command = new AddExclusionCommand(req.UserId, req.TypeExclusion, req.StartDatetime, req.EndDatetime);
        var exclusion = await exclusions.AddAsync(command, ct);
        await Send.OkAsync(exclusion.ToResponse(), ct);
    }
}

/// <summary>POST /work_time/get_exclusion — list a worker's exclusions.</summary>
public sealed class GetExclusionsEndpoint(IExclusionService exclusions) : Endpoint<UserIdRequest, ExclusionsResponse>
{
    public override void Configure()
    {
        Post("/work_time/get_exclusion");
        AllowAnonymous();
        Description(b => b
            .Produces<ExclusionsResponse>(200)
            .ProducesProblem(400).ProducesProblem(404));
        Summary(s => s.Summary = "Get all exclusions from the work schedule.");
    }

    public override async Task HandleAsync(UserIdRequest req, CancellationToken ct)
    {
        var list = await exclusions.GetByUserAsync(req.UserId, ct);
        var response = new ExclusionsResponse(req.UserId, list.Select(e => e.ToResponse()).ToArray());
        await Send.OkAsync(response, ct);
    }
}
