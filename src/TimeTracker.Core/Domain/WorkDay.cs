namespace TimeTracker.Core;

/// <summary>
/// The attendance record for one user on one calendar day: holds the arrival and departure taps and
/// the worked duration. Enforces the "exactly two taps per day" rule — a third tap throws
/// <see cref="AlreadyTappedException"/>. Lateness/early-departure are derived elsewhere
/// (<see cref="AttendanceEvaluator"/>) because they need the schedule, grace, and exclusions.
/// </summary>
public sealed record WorkDay
{
    public Guid Id { get; private set; }

    public long UserId { get; private set; }

    public DateOnly Date { get; private set; }

    public DateTimeOffset? ArrivalAt { get; private set; } // first tap

    public DateTimeOffset? DepartureAt { get; private set; } // second tap

    private WorkDay()
    {
    }

    /// <summary>Opens a new work day and records its first (arrival) tap.</summary>
    public static WorkDay Open(long userId, DateOnly date, DateTimeOffset arrivalAt)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), userId, "User id must be positive.");
        }

        var day = new WorkDay
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Date = date,
        };
        day.RecordTap(arrivalAt);
        return day;
    }

    /// <summary>
    /// Records the next tap: the first becomes the arrival, the second the departure.
    /// Returns which kind was recorded. Throws <see cref="AlreadyTappedException"/> when both taps
    /// already exist, and <see cref="InvalidTapException"/> when a departure precedes the arrival.
    /// </summary>
    public TapKind RecordTap(DateTimeOffset at)
    {
        if (ArrivalAt is null)
        {
            ArrivalAt = at;
            return TapKind.Arrival;
        }

        if (DepartureAt is null)
        {
            if (at < ArrivalAt.Value)
            {
                throw new InvalidTapException("Departure must not be earlier than arrival.");
            }

            DepartureAt = at;
            return TapKind.Departure;
        }

        throw new AlreadyTappedException(UserId, Date);
    }

    /// <summary>True once both taps are present.</summary>
    public bool IsComplete => ArrivalAt is not null && DepartureAt is not null;

    /// <summary>Worked duration (departure − arrival), or null when the day is not yet complete.</summary>
    public TimeSpan? WorkedDuration => ArrivalAt is { } a && DepartureAt is { } d ? d - a : null;
}
