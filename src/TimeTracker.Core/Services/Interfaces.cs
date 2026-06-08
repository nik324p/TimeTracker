namespace TimeTracker.Core;

/// <summary>Backs <c>/card/touch</c>.</summary>
public interface ITapService
{
    Task<TapResult> TouchAsync(string cardUid, CancellationToken ct = default);
}

/// <summary>Backs <c>/card/assign</c>, <c>/card/delete</c>, <c>/card/list_by_user</c>, <c>/card/delete_all_by_user</c>.</summary>
public interface ICardService
{
    Task<CardAssignment> AssignAsync(long userId, string cardUid, CancellationToken ct = default);

    Task<CardAssignment> DeleteAsync(string cardUid, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListByUserAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> DeleteAllByUserAsync(long userId, CancellationToken ct = default);
}

/// <summary>Backs <c>/work_time/set</c>, <c>/work_time/get</c>.</summary>
public interface IScheduleService
{
    Task<WorkSchedule> SetAsync(SetScheduleCommand command, CancellationToken ct = default);

    Task<WorkSchedule> GetAsync(long userId, CancellationToken ct = default);
}

/// <summary>Backs <c>/work_time/add_exclusion</c>, <c>/work_time/get_exclusion</c>.</summary>
public interface IExclusionService
{
    Task<ScheduleExclusion> AddAsync(AddExclusionCommand command, CancellationToken ct = default);

    Task<IReadOnlyList<ScheduleExclusion>> GetByUserAsync(long userId, CancellationToken ct = default);
}

/// <summary>Backs <c>/work_time/history_by_user</c>, <c>/work_time/history</c>.</summary>
public interface IHistoryService
{
    Task<IReadOnlyList<WorkDayHistoryEntry>> GetByUserAsync(
        long userId, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default);

    Task<IReadOnlyList<WorkDayHistoryEntry>> GetAllAsync(
        int limit, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default);
}

/// <summary>Backs <c>/work_time/statistics_by_user</c>, <c>/work_time/statistics</c>.</summary>
public interface IStatisticsService
{
    Task<UserStatistics> GetByUserAsync(
        long userId, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default);

    Task<IReadOnlyList<UserStatistics>> GetSummaryAsync(
        int limit, StatisticsPeriod period = StatisticsPeriod.Month, CancellationToken ct = default);
}
