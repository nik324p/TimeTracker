namespace TimeTracker.Core;

/// <summary>
/// The commit boundary. Implemented by the EF Core DbContext in Infrastructure; services stage
/// changes via the repositories below and call <see cref="SaveChangesAsync"/> once.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> FindAsync(long userId, CancellationToken ct = default);

    Task<bool> ExistsAsync(long userId, CancellationToken ct = default);

    /// <summary>Up to <paramref name="limit"/> user ids, newest-active first (for /history, /statistics).</summary>
    Task<IReadOnlyList<long>> ListUserIdsAsync(int limit, CancellationToken ct = default);

    void Add(User user);
}

public interface ICardRepository
{
    /// <summary>Caller passes a normalized UID (see <see cref="CardUidNormalizer"/>).</summary>
    Task<Card?> FindByUidAsync(string cardUid, CancellationToken ct = default);

    Task<IReadOnlyList<Card>> ListByUserAsync(long userId, CancellationToken ct = default);

    void Add(Card card);

    void Remove(Card card);

    void RemoveRange(IEnumerable<Card> cards);
}

public interface IWorkScheduleRepository
{
    Task<WorkSchedule?> FindByUserAsync(long userId, CancellationToken ct = default);

    void Add(WorkSchedule schedule);

    // Updates flow through change-tracking after WorkSchedule.Update(); no explicit method needed.
}

public interface IScheduleExclusionRepository
{
    Task<IReadOnlyList<ScheduleExclusion>> ListByUserAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<ScheduleExclusion>> ListByUserInRangeAsync(
        long userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);

    void Add(ScheduleExclusion exclusion);
}

public interface IWorkDayRepository
{
    Task<WorkDay?> FindByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct = default);

    Task<IReadOnlyList<WorkDay>> ListByUserAsync(long userId, CancellationToken ct = default);

    Task<IReadOnlyList<WorkDay>> ListByUserInRangeAsync(
        long userId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Newest-first, capped at <paramref name="limit"/>, across all users (for /history).</summary>
    Task<IReadOnlyList<WorkDay>> ListAllAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Newest-first within <c>[from, to]</c>, capped at <paramref name="limit"/>, across all users
    /// (for /history scoped to a period).
    /// </summary>
    Task<IReadOnlyList<WorkDay>> ListAllInRangeAsync(
        DateOnly from, DateOnly to, int limit, CancellationToken ct = default);

    void Add(WorkDay workDay);

    // Mutation of an existing WorkDay (second tap) flows through change-tracking after RecordTap().
}
