namespace TimeTracker.Tests;

public sealed class ScheduleExclusionTests
{
    [Fact]
    public void Create_EndBeforeStart_ThrowsInvalidExclusion()
    {
        // Act
        var act = () => ScheduleExclusion.Create(
            1001, ExclusionType.ArriveLater, TestClock.On(new(12, 0)), TestClock.On(new(8, 0)));

        // Assert
        act.Should().Throw<InvalidExclusionException>().Which.Code.Should().Be("invalid_exclusion");
    }

    [Fact]
    public void Covers_DateInsideRange_True()
    {
        // Arrange
        var exclusion = new ScheduleExclusionBuilder().CoveringDate(TestClock.DefaultDate).Build();

        // Act / Assert
        exclusion.Covers(TestClock.DefaultDate).Should().BeTrue();
        exclusion.Covers(TestClock.DefaultDate.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void AppliesTo_InstantInsideRange_True()
    {
        // Arrange
        var exclusion = new ScheduleExclusionBuilder()
            .From(TestClock.On(new(9, 0))).To(TestClock.On(new(12, 0))).Build();

        // Act / Assert
        exclusion.AppliesTo(TestClock.On(new(10, 0))).Should().BeTrue();
        exclusion.AppliesTo(TestClock.On(new(13, 0))).Should().BeFalse();
    }
}
