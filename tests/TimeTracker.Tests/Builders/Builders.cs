namespace TimeTracker.Tests;

/// <summary>Valid-by-default builders going through the real Core factories so invariants are exercised.</summary>
public sealed class UserBuilder
{
    private long _id = 1001;
    private string? _displayName = "Test Employee";

    public UserBuilder WithId(long id) { _id = id; return this; }

    public UserBuilder WithName(string? name) { _displayName = name; return this; }

    public User Build() => User.Create(_id, _displayName);
}

public sealed class CardBuilder
{
    private string _uid = "CARD-0001";
    private long _userId = 1001;
    private TimeProvider _clock = TestClock.AtNineAm();

    public CardBuilder WithUid(string uid) { _uid = uid; return this; }

    public CardBuilder AssignedTo(long userId) { _userId = userId; return this; }

    public CardBuilder WithClock(TimeProvider clock) { _clock = clock; return this; }

    public Card Build() => Card.Assign(_uid, _userId, _clock);
}

public sealed class WorkScheduleBuilder
{
    private long _userId = 1001;
    private TimeOnly _start = new(9, 0);
    private TimeOnly _end = new(18, 0);
    private WorkDays _days = WorkDays.Weekdays;
    private bool _free;

    public WorkScheduleBuilder ForUser(long userId) { _userId = userId; return this; }

    public WorkScheduleBuilder StartingAt(TimeOnly t) { _start = t; return this; }

    public WorkScheduleBuilder EndingAt(TimeOnly t) { _end = t; return this; }

    public WorkScheduleBuilder WithDays(WorkDays days) { _days = days; return this; }

    public WorkScheduleBuilder FreeSchedule() { _free = true; return this; }

    public WorkSchedule Build() => WorkSchedule.Create(_userId, _start, _end, _days, _free);
}

public sealed class WorkDayBuilder
{
    private long _userId = 1001;
    private DateOnly _date = TestClock.DefaultDate;
    private DateTimeOffset? _arrival;
    private DateTimeOffset? _departure;

    public WorkDayBuilder ForUser(long userId) { _userId = userId; return this; }

    public WorkDayBuilder OnDate(DateOnly date) { _date = date; return this; }

    public WorkDayBuilder WithArrivalAt(DateTimeOffset at) { _arrival = at; return this; }

    public WorkDayBuilder WithDepartureAt(DateTimeOffset at) { _departure = at; return this; }

    public WorkDayBuilder ArrivedAt(TimeOnly t) { _arrival = At(t); return this; }

    public WorkDayBuilder DepartedAt(TimeOnly t) { _departure = At(t); return this; }

    /// <summary>On-time arrival (09:00) and departure (18:00).</summary>
    public WorkDayBuilder WithTwoTaps() => ArrivedAt(new(9, 0)).DepartedAt(new(18, 0));

    public WorkDayBuilder LateBy(int minutes) => ArrivedAt(new TimeOnly(9, 0).AddMinutes(minutes));

    public WorkDayBuilder LeftEarlyBy(int minutes) => DepartedAt(new TimeOnly(18, 0).AddMinutes(-minutes));

    public WorkDay Build()
    {
        // A WorkDay always opens on its first (arrival) tap; default to an on-time 09:00 arrival.
        var arrival = _arrival ?? At(new(9, 0));
        var day = WorkDay.Open(_userId, _date, arrival);
        if (_departure is { } departure)
        {
            day.RecordTap(departure);
        }

        return day;
    }

    private DateTimeOffset At(TimeOnly time) => TestClock.On(_date, time);
}

public sealed class ScheduleExclusionBuilder
{
    private long _userId = 1001;
    private ExclusionType _type = ExclusionType.FullWorkingDay;
    private DateTimeOffset _start = TestClock.On(TimeOnly.MinValue);
    private DateTimeOffset _end = TestClock.On(new(23, 59, 59));

    public ScheduleExclusionBuilder ForUser(long userId) { _userId = userId; return this; }

    public ScheduleExclusionBuilder OfType(ExclusionType type) { _type = type; return this; }

    public ScheduleExclusionBuilder ArriveLater() { _type = ExclusionType.ArriveLater; return this; }

    public ScheduleExclusionBuilder LeaveEarlier() { _type = ExclusionType.LeaveEarlier; return this; }

    public ScheduleExclusionBuilder FullWorkingDay() { _type = ExclusionType.FullWorkingDay; return this; }

    public ScheduleExclusionBuilder From(DateTimeOffset start) { _start = start; return this; }

    public ScheduleExclusionBuilder To(DateTimeOffset end) { _end = end; return this; }

    /// <summary>Make the exclusion span the whole given calendar day.</summary>
    public ScheduleExclusionBuilder CoveringDate(DateOnly date)
    {
        _start = TestClock.On(date, TimeOnly.MinValue);
        _end = TestClock.On(date, new(23, 59, 59));
        return this;
    }

    public ScheduleExclusion Build() => ScheduleExclusion.Create(_userId, _type, _start, _end);
}
