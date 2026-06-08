namespace TimeTracker.Core;

/// <summary>Adds and reads a worker's schedule exclusions.</summary>
public sealed class ExclusionService(
    IUserRepository users,
    IScheduleExclusionRepository exclusions,
    IUnitOfWork unitOfWork) : IExclusionService
{
    public async Task<ScheduleExclusion> AddAsync(AddExclusionCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await users.EnsureExistsAsync(command.UserId, ct);

        var exclusion = ScheduleExclusion.Create(
            command.UserId, command.Type, command.StartDateTime, command.EndDateTime);
        exclusions.Add(exclusion);
        await unitOfWork.SaveChangesAsync(ct);

        return exclusion;
    }

    public async Task<IReadOnlyList<ScheduleExclusion>> GetByUserAsync(long userId, CancellationToken ct = default)
    {
        await users.EnsureExistsAsync(userId, ct);

        return await exclusions.ListByUserAsync(userId, ct);
    }
}
