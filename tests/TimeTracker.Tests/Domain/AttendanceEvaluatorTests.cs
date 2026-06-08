namespace TimeTracker.Tests;

public sealed class AttendanceEvaluatorTests
{
    private static DayEvaluation Evaluate(
        WorkDay day, WorkSchedule schedule, int graceMinutes = 5, params ScheduleExclusion[] exclusions)
        => AttendanceEvaluator.Evaluate(day, schedule, exclusions, new LatenessOptions { GraceMinutes = graceMinutes });

    [Fact]
    public void Evaluate_OnTimeFullDay_NoFlags_NoUnderWork()
    {
        var day = new WorkDayBuilder().WithTwoTaps().Build();
        var schedule = new WorkScheduleBuilder().Build();

        var result = Evaluate(day, schedule);

        result.WasLate.Should().BeFalse();
        result.LeftEarly.Should().BeFalse();
        result.UnderWorked.Should().Be(TimeSpan.Zero);
        result.ExpectedDuration.Should().Be(TimeSpan.FromHours(9));
        result.WorkedDuration.Should().Be(TimeSpan.FromHours(9));
    }

    // (graceMinutes, minutesAfterStart, expectedLate) — boundary uses '>', so equal is NOT late.
    public static TheoryData<int, int, bool> LatenessGraceCases => new()
    {
        { 10, 0, false },
        { 10, 5, false },
        { 10, 10, false }, // exactly at the grace boundary
        { 10, 11, true },
        { 0, 1, true },
    };

    [Theory]
    [MemberData(nameof(LatenessGraceCases))]
    public void Evaluate_Arrival_LatenessRespectsGrace(int grace, int minutesAfter, bool expectedLate)
    {
        var day = new WorkDayBuilder().LateBy(minutesAfter).DepartedAt(new(18, 0)).Build();
        var schedule = new WorkScheduleBuilder().Build();

        var result = Evaluate(day, schedule, grace);

        result.WasLate.Should().Be(expectedLate);
    }

    [Fact]
    public void Evaluate_LateArrival_CoveredByArriveLater_IsExcused()
    {
        var day = new WorkDayBuilder().LateBy(30).DepartedAt(new(18, 0)).Build();
        var schedule = new WorkScheduleBuilder().Build();
        var exclusion = new ScheduleExclusionBuilder().ArriveLater()
            .From(TestClock.On(new(9, 0))).To(TestClock.On(new(12, 0))).Build();

        var result = Evaluate(day, schedule, 5, exclusion);

        result.WasLate.Should().BeTrue();
        result.LateExcused.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_EarlyDeparture_BeforeEndMinusGrace_FlagsLeftEarly()
    {
        var day = new WorkDayBuilder().ArrivedAt(new(9, 0)).LeftEarlyBy(30).Build();
        var schedule = new WorkScheduleBuilder().Build();

        var result = Evaluate(day, schedule);

        result.LeftEarly.Should().BeTrue();
        result.LeftEarlyExcused.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_EarlyDeparture_CoveredByLeaveEarlier_IsExcused()
    {
        var day = new WorkDayBuilder().ArrivedAt(new(9, 0)).LeftEarlyBy(60).Build();
        var schedule = new WorkScheduleBuilder().Build();
        var exclusion = new ScheduleExclusionBuilder().LeaveEarlier()
            .From(TestClock.On(new(16, 0))).To(TestClock.On(new(18, 0))).Build();

        var result = Evaluate(day, schedule, 5, exclusion);

        result.LeftEarly.Should().BeTrue();
        result.LeftEarlyExcused.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_FullWorkingDayExclusion_ZeroExpected_NoFlags_WorkedCredited()
    {
        var day = new WorkDayBuilder().LateBy(120).DepartedAt(new(18, 0)).Build();
        var schedule = new WorkScheduleBuilder().Build();
        var exclusion = new ScheduleExclusionBuilder().FullWorkingDay().CoveringDate(TestClock.DefaultDate).Build();

        var result = Evaluate(day, schedule, 5, exclusion);

        result.ExpectedDuration.Should().Be(TimeSpan.Zero);
        result.UnderWorked.Should().Be(TimeSpan.Zero);
        result.WasLate.Should().BeFalse();
        result.WorkedDuration.Should().BeGreaterThan(TimeSpan.Zero); // still credited
    }

    [Fact]
    public void Evaluate_NonWorkingDay_ZeroExpected_NoFlags()
    {
        var saturday = new DateOnly(2026, 6, 6);
        var day = new WorkDayBuilder().OnDate(saturday).ArrivedAt(new(10, 0)).DepartedAt(new(14, 0)).Build();
        var schedule = new WorkScheduleBuilder().WithDays(WorkDays.Weekdays).Build();

        var result = Evaluate(day, schedule);

        result.IsWorkingDay.Should().BeFalse();
        result.ExpectedDuration.Should().Be(TimeSpan.Zero);
        result.WasLate.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_FreeSchedule_NeverLateOrEarly()
    {
        var day = new WorkDayBuilder().LateBy(120).LeftEarlyBy(120).Build();
        var schedule = new WorkScheduleBuilder().FreeSchedule().Build();

        var result = Evaluate(day, schedule);

        result.WasLate.Should().BeFalse();
        result.LeftEarly.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WorkedLessThanExpected_ComputesUnderWork()
    {
        var day = new WorkDayBuilder().ArrivedAt(new(9, 0)).DepartedAt(new(13, 0)).Build();
        var schedule = new WorkScheduleBuilder().Build(); // expects 9h

        var result = Evaluate(day, schedule);

        result.WorkedDuration.Should().Be(TimeSpan.FromHours(4));
        result.UnderWorked.Should().Be(TimeSpan.FromHours(5));
    }
}
