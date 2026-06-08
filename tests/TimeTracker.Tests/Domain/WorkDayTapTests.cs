namespace TimeTracker.Tests;

public sealed class WorkDayTapTests
{
    [Fact]
    public void Open_FirstTap_RecordsArrival()
    {
        // Arrange
        var arrival = TestClock.On(new(9, 0));

        // Act
        var day = WorkDay.Open(1001, TestClock.DefaultDate, arrival);

        // Assert
        day.ArrivalAt.Should().Be(arrival);
        day.DepartureAt.Should().BeNull();
        day.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void RecordTap_SecondTap_RecordsDeparture()
    {
        // Arrange
        var day = new WorkDayBuilder().ArrivedAt(new(9, 0)).Build();
        var departure = TestClock.On(new(18, 0));

        // Act
        var kind = day.RecordTap(departure);

        // Assert
        kind.Should().Be(TapKind.Departure);
        day.DepartureAt.Should().Be(departure);
        day.IsComplete.Should().BeTrue();
        day.WorkedDuration.Should().Be(TimeSpan.FromHours(9));
    }

    [Fact]
    public void RecordTap_ThirdTapSameDay_ThrowsAlreadyTapped()
    {
        // Arrange — a day that already has both taps
        var day = new WorkDayBuilder().WithTwoTaps().Build();

        // Act
        var act = () => day.RecordTap(TestClock.On(new(19, 0)));

        // Assert
        act.Should().Throw<AlreadyTappedException>()
            .Which.Code.Should().Be("already_tapped");
    }

    [Fact]
    public void RecordTap_DepartureBeforeArrival_ThrowsInvalidTap()
    {
        // Arrange
        var day = new WorkDayBuilder().ArrivedAt(new(9, 0)).Build();

        // Act — a departure earlier than the arrival
        var act = () => day.RecordTap(TestClock.On(new(8, 0)));

        // Assert
        act.Should().Throw<InvalidTapException>()
            .Which.Code.Should().Be("invalid_tap");
    }

    [Fact]
    public void WorkedDuration_OpenDay_IsNull()
    {
        // Arrange / Act
        var day = new WorkDayBuilder().ArrivedAt(new(9, 0)).Build();

        // Assert
        day.WorkedDuration.Should().BeNull();
    }
}
