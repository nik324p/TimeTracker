namespace TimeTracker.Tests;

public sealed class StatisticsServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWorkDayRepository _workDays = Substitute.For<IWorkDayRepository>();
    private readonly IWorkScheduleRepository _schedules = Substitute.For<IWorkScheduleRepository>();
    private readonly IScheduleExclusionRepository _exclusions = Substitute.For<IScheduleExclusionRepository>();
    private readonly FakeTimeProvider _clock = TestClock.AtNineAm(); // today = Thu 2026-06-04

    private StatisticsService Sut() =>
        new(_users, _workDays, _schedules, _exclusions, _clock, new LatenessOptions(), new HistoryOptions());

    private void GivenUserWith(WorkSchedule? schedule, WorkDay[]? days = null, ScheduleExclusion[]? exclusions = null)
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>()).Returns(schedule);
        _workDays.ListByUserInRangeAsync(1001, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(days ?? []);
        _exclusions.ListByUserInRangeAsync(1001, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(exclusions ?? []);
    }

    [Fact]
    public async Task GetByUser_UserMissing_ThrowsUserNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().GetByUserAsync(1001);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task GetByUser_NoSchedule_ThrowsScheduleNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>()).Returns((WorkSchedule?)null);

        var act = () => Sut().GetByUserAsync(1001);

        await act.Should().ThrowAsync<ScheduleNotFoundException>();
    }

    [Fact]
    public async Task GetByUser_Month_RequiredHoursSumScheduledWorkingDays()
    {
        // Month window Jun 1–4 2026 = Mon..Thu = 4 working days × 9h = 36h required; no taps ⇒ all under-worked.
        GivenUserWith(new WorkScheduleBuilder().ForUser(1001).Build());

        var stats = await Sut().GetByUserAsync(1001, StatisticsPeriod.Month);

        stats.RequiredToWork.Should().Be(TimeSpan.FromHours(36));
        stats.Worked.Should().Be(TimeSpan.Zero);
        stats.UnderWorked.Should().Be(TimeSpan.FromHours(36));
        stats.FromDate.Should().Be(new DateOnly(2026, 6, 1));
        stats.ToDate.Should().Be(new DateOnly(2026, 6, 4));
    }

    [Fact]
    public async Task GetByUser_LateDayWithoutExclusion_CountsLateWithoutReason()
    {
        var lateDay = new WorkDayBuilder().ForUser(1001).OnDate(new(2026, 6, 4)).LateBy(30).DepartedAt(new(18, 0)).Build();
        GivenUserWith(new WorkScheduleBuilder().ForUser(1001).Build(), [lateDay]);

        var stats = await Sut().GetByUserAsync(1001, StatisticsPeriod.Month);

        stats.TimesLateWithoutReason.Should().Be(1);
        stats.TimesLateWithReason.Should().Be(0);
        stats.Worked.Should().Be(TimeSpan.FromHours(8.5));
    }

    [Fact]
    public async Task GetByUser_LateDayCoveredByArriveLater_CountsLateWithReason()
    {
        var lateDay = new WorkDayBuilder().ForUser(1001).OnDate(new(2026, 6, 4)).LateBy(30).DepartedAt(new(18, 0)).Build();
        var exclusion = new ScheduleExclusionBuilder().ForUser(1001).ArriveLater()
            .From(TestClock.On(new(2026, 6, 4), new(9, 0))).To(TestClock.On(new(2026, 6, 4), new(12, 0))).Build();
        GivenUserWith(new WorkScheduleBuilder().ForUser(1001).Build(), [lateDay], [exclusion]);

        var stats = await Sut().GetByUserAsync(1001, StatisticsPeriod.Month);

        stats.TimesLateWithReason.Should().Be(1);
        stats.TimesLateWithoutReason.Should().Be(0);
    }

    [Fact]
    public async Task GetByUser_FreeSchedule_NeverCountsLate()
    {
        var lateDay = new WorkDayBuilder().ForUser(1001).OnDate(new(2026, 6, 4)).LateBy(120).DepartedAt(new(18, 0)).Build();
        GivenUserWith(new WorkScheduleBuilder().ForUser(1001).FreeSchedule().Build(), [lateDay]);

        var stats = await Sut().GetByUserAsync(1001, StatisticsPeriod.Month);

        stats.TimesLateWithoutReason.Should().Be(0);
        stats.TimesLateWithReason.Should().Be(0);
    }

    [Fact]
    public async Task GetByUser_NoPeriodGiven_DefaultsToMonth()
    {
        GivenUserWith(new WorkScheduleBuilder().ForUser(1001).Build());

        var stats = await Sut().GetByUserAsync(1001);

        stats.Period.Should().Be(StatisticsPeriod.Month);
        stats.FromDate.Should().Be(new DateOnly(2026, 6, 1));
    }

    // today = Thu 2026-06-04. Week→Monday 06-01, Month→06-01, Year→01-01, Entire→null.
    public static TheoryData<StatisticsPeriod, DateOnly?> PeriodWindows => new()
    {
        { StatisticsPeriod.Week, new DateOnly(2026, 6, 1) },
        { StatisticsPeriod.Month, new DateOnly(2026, 6, 1) },
        { StatisticsPeriod.Year, new DateOnly(2026, 1, 1) },
        { StatisticsPeriod.EntirePeriod, null },
    };

    [Theory]
    [MemberData(nameof(PeriodWindows))]
    public async Task GetByUser_Period_ResolvesFromDate(StatisticsPeriod period, DateOnly? expectedFrom)
    {
        GivenUserWith(new WorkScheduleBuilder().ForUser(1001).Build());

        var stats = await Sut().GetByUserAsync(1001, period);

        stats.FromDate.Should().Be(expectedFrom);
        stats.ToDate.Should().Be(new DateOnly(2026, 6, 4));
    }

    [Fact]
    public async Task GetSummary_LimitBelowOne_ThrowsInvalidLimit()
    {
        var act = () => Sut().GetSummaryAsync(0);

        await act.Should().ThrowAsync<InvalidLimitException>();
    }

    [Fact]
    public async Task GetSummary_SkipsUsersWithoutSchedule()
    {
        _users.ListUserIdsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([1001L, 1002L]);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>())
            .Returns(new WorkScheduleBuilder().ForUser(1001).Build());
        _schedules.FindByUserAsync(1002, Arg.Any<CancellationToken>()).Returns((WorkSchedule?)null);
        _workDays.ListByUserInRangeAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _exclusions.ListByUserInRangeAsync(Arg.Any<long>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await Sut().GetSummaryAsync(10);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(1001);
    }
}
