namespace TimeTracker.Core;

/// <summary>
/// Base type for all domain rule violations. The Api's global exception handler pattern-matches
/// on the concrete type and uses <see cref="Code"/> for the ProblemDetails type/title mapping.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Machine-readable, snake_case error code (e.g. <c>already_tapped</c>).</summary>
    public abstract string Code { get; }

    protected DomainException(string message) : base(message)
    {
    }
}

/// <summary>A referenced user id has no record.</summary>
public sealed class UserNotFoundException(long userId)
    : DomainException($"User {userId} was not found.")
{
    public long UserId { get; } = userId;

    public override string Code => ErrorCodes.UserNotFound;
}

/// <summary>A card UID is not bound to any user (touch/delete of an unknown card).</summary>
public sealed class CardNotFoundException(string cardUid)
    : DomainException($"Card '{cardUid}' was not found.")
{
    public string CardUid { get; } = cardUid;

    public override string Code => ErrorCodes.CardNotFound;
}

/// <summary>Assigning a card UID that is already bound to a user.</summary>
public sealed class CardAlreadyAssignedException(string cardUid)
    : DomainException($"Card '{cardUid}' is already assigned.")
{
    public string CardUid { get; } = cardUid;

    public override string Code => ErrorCodes.CardAlreadyAssigned;
}

/// <summary>A third tap on a day that already has both an arrival and a departure.</summary>
public sealed class AlreadyTappedException(long userId, DateOnly date)
    : DomainException($"User {userId} has already tapped both arrival and departure on {date:yyyy-MM-dd}.")
{
    public long UserId { get; } = userId;

    public DateOnly Date { get; } = date;

    public override string Code => ErrorCodes.AlreadyTapped;
}

/// <summary>A tap violates ordering (e.g. a departure earlier than the arrival).</summary>
public sealed class InvalidTapException(string reason)
    : DomainException(reason)
{
    public override string Code => ErrorCodes.InvalidTap;
}

/// <summary>No work schedule is set for the user (get schedule, or statistics).</summary>
public sealed class ScheduleNotFoundException(long userId)
    : DomainException($"No work schedule is set for user {userId}.")
{
    public long UserId { get; } = userId;

    public override string Code => ErrorCodes.ScheduleNotFound;
}

/// <summary>A schedule's times/days are invalid (end &lt;= start, no working days, …).</summary>
public sealed class InvalidScheduleException(string reason)
    : DomainException(reason)
{
    public override string Code => ErrorCodes.InvalidSchedule;
}

/// <summary>An exclusion's range is invalid (end before start) or its type is unknown.</summary>
public sealed class InvalidExclusionException(string reason)
    : DomainException(reason)
{
    public override string Code => ErrorCodes.InvalidExclusion;
}

/// <summary>A history/statistics <c>limit</c> is less than 1.</summary>
public sealed class InvalidLimitException(int limit)
    : DomainException($"Limit must be at least 1, but was {limit}.")
{
    public int Limit { get; } = limit;

    public override string Code => ErrorCodes.InvalidLimit;
}
