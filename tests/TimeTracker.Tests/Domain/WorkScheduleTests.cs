namespace TimeTracker.Tests;

public sealed class WorkScheduleTests
{
    [Fact]
    public void Create_ValidSchedule_SetsFields()
    {
        // Act
        var schedule = new WorkScheduleBuilder()
            .StartingAt(new(9, 0)).EndingAt(new(18, 0)).WithDays(WorkDays.Weekdays).Build();

        // Assert
        schedule.ExpectedDuration.Should().Be(TimeSpan.FromHours(9));
    }

    [Fact]
    public void Create_EndNotAfterStart_ThrowsInvalidSchedule()
    {
        // Act
        var act = () => WorkSchedule.Create(1001, new(18, 0), new(9, 0), WorkDays.Weekdays, freeSchedule: false);

        // Assert
        act.Should().Throw<InvalidScheduleException>().Which.Code.Should().Be("invalid_schedule");
    }

    [Fact]
    public void Create_NoWorkingDays_ThrowsInvalidSchedule()
    {
        // Act
        var act = () => WorkSchedule.Create(1001, new(9, 0), new(18, 0), WorkDays.None, freeSchedule: false);

        // Assert
        act.Should().Throw<InvalidScheduleException>();
    }

    [Theory]
    [InlineData("2026-06-04", true)]  // Thursday — a weekday
    [InlineData("2026-06-06", false)] // Saturday
    public void IsWorkingDay_WeekdaySchedule_MatchesDayOfWeek(string date, bool expected)
    {
        // Arrange
        var schedule = new WorkScheduleBuilder().WithDays(WorkDays.Weekdays).Build();

        // Act / Assert
        schedule.IsWorkingDay(DateOnly.Parse(date)).Should().Be(expected);
    }

    [Fact]
    public void Update_ChangesFields_AndReEnforcesInvariants()
    {
        // Arrange
        var schedule = new WorkScheduleBuilder().Build();

        // Act
        schedule.Update(new(8, 0), new(16, 0), WorkDays.All, freeSchedule: true);

        // Assert
        schedule.StartTime.Should().Be(new TimeOnly(8, 0));
        schedule.FreeSchedule.Should().BeTrue();
        schedule.ExpectedDuration.Should().Be(TimeSpan.FromHours(8));
    }
}
