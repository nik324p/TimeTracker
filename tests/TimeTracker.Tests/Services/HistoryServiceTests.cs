namespace TimeTracker.Tests;

public sealed class HistoryServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWorkDayRepository _workDays = Substitute.For<IWorkDayRepository>();
    private readonly IWorkScheduleRepository _schedules = Substitute.For<IWorkScheduleRepository>();
    private readonly IScheduleExclusionRepository _exclusions = Substitute.For<IScheduleExclusionRepository>();
    private readonly FakeTimeProvider _clock = TestClock.AtNineAm(); // today = Thu 2026-06-04
    private readonly HistoryOptions _historyOptions = new() { MaxLimit = 500 };

    private HistoryService Sut() =>
        new(_users, _workDays, _schedules, _exclusions, _clock, new LatenessOptions(), _historyOptions);

    [Fact]
    public async Task GetByUser_UserMissing_ThrowsUserNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().GetByUserAsync(1001);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task GetByUser_WithSchedule_DerivesLatenessFlags()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>())
            .Returns(new WorkScheduleBuilder().ForUser(1001).Build());
        _exclusions.ListByUserAsync(1001, Arg.Any<CancellationToken>()).Returns([]);
        _workDays.ListByUserAsync(1001, Arg.Any<CancellationToken>())
            .Returns([new WorkDayBuilder().ForUser(1001).LateBy(30).DepartedAt(new(18, 0)).Build()]);

        // EntirePeriod ⇒ no window, so the projection sees every recorded day (ListByUserAsync).
        var result = await Sut().GetByUserAsync(1001, StatisticsPeriod.EntirePeriod);

        result.Should().HaveCount(1);
        result[0].WasLate.Should().BeTrue();
        result[0].UserId.Should().Be(1001);
    }

    [Fact]
    public async Task GetByUser_NoSchedule_FlagsAreFalse()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>()).Returns((WorkSchedule?)null);
        _exclusions.ListByUserAsync(1001, Arg.Any<CancellationToken>()).Returns([]);
        _workDays.ListByUserAsync(1001, Arg.Any<CancellationToken>())
            .Returns([new WorkDayBuilder().ForUser(1001).LateBy(120).DepartedAt(new(18, 0)).Build()]);

        var result = await Sut().GetByUserAsync(1001, StatisticsPeriod.EntirePeriod);

        result[0].WasLate.Should().BeFalse();
        result[0].ArrivalAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByUser_NoPeriodGiven_DefaultsToMonthWindow()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>())
            .Returns(new WorkScheduleBuilder().ForUser(1001).Build());
        // Bounded period ⇒ exclusions are fetched windowed too (same as StatisticsService).
        _exclusions.ListByUserInRangeAsync(1001, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workDays.ListByUserInRangeAsync(1001, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new WorkDayBuilder().ForUser(1001).WithTwoTaps().Build()]);

        var result = await Sut().GetByUserAsync(1001);

        result.Should().HaveCount(1);
        // Month of today (Thu 2026-06-04) → Jun 1..Jun 4; range queries, not the all-days queries.
        await _workDays.Received(1).ListByUserInRangeAsync(
            1001, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 4), Arg.Any<CancellationToken>());
        await _workDays.DidNotReceive().ListByUserAsync(1001, Arg.Any<CancellationToken>());
        await _exclusions.DidNotReceive().ListByUserAsync(1001, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_LimitBelowOne_ThrowsInvalidLimit()
    {
        var act = () => Sut().GetAllAsync(0);

        await act.Should().ThrowAsync<InvalidLimitException>();
    }

    [Fact]
    public async Task GetAll_EntirePeriod_LimitAboveMax_ClampsToMaxLimit()
    {
        _workDays.ListAllAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        await Sut().GetAllAsync(1000, StatisticsPeriod.EntirePeriod);

        await _workDays.Received(1).ListAllAsync(500, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_Month_QueriesRangeWithClampedLimit()
    {
        _workDays.ListAllInRangeAsync(
            Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        await Sut().GetAllAsync(1000); // default month

        await _workDays.Received(1).ListAllInRangeAsync(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 4), 500, Arg.Any<CancellationToken>());
        await _workDays.DidNotReceive().ListAllAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
