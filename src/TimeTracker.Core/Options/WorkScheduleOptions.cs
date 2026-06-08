namespace TimeTracker.Core;

/// <summary>Default work-hours used when schedule fields are omitted (convenience defaults).</summary>
public sealed class WorkScheduleOptions
{
    public const string SectionName = "WorkSchedule";

    public TimeOnly DefaultStartTime { get; set; } = new(9, 0);

    public TimeOnly DefaultEndTime { get; set; } = new(18, 0);

    public WorkDays DefaultDays { get; set; } = WorkDays.Weekdays;
}
