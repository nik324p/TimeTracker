namespace TimeTracker.Tests;

public sealed class ScheduleServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWorkScheduleRepository _schedules = Substitute.For<IWorkScheduleRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ScheduleService Sut() => new(_users, _schedules, _uow);

    private static SetScheduleCommand ValidCommand(long userId = 1001) =>
        new(userId, new(9, 0), new(18, 0), WorkDays.Weekdays, FreeSchedule: false);

    [Fact]
    public async Task Set_UserMissing_ThrowsUserNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().SetAsync(ValidCommand());

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Set_NoExistingSchedule_CreatesAndSaves()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>()).Returns((WorkSchedule?)null);

        var result = await Sut().SetAsync(ValidCommand());

        result.UserId.Should().Be(1001);
        _schedules.Received(1).Add(Arg.Any<WorkSchedule>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_ExistingSchedule_UpdatesInPlace_DoesNotAdd()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        var existing = new WorkScheduleBuilder().ForUser(1001).StartingAt(new(9, 0)).Build();
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Sut().SetAsync(
            new SetScheduleCommand(1001, new(8, 0), new(16, 0), WorkDays.All, FreeSchedule: true));

        result.StartTime.Should().Be(new TimeOnly(8, 0));
        result.FreeSchedule.Should().BeTrue();
        _schedules.DidNotReceive().Add(Arg.Any<WorkSchedule>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_InvalidTimes_ThrowsInvalidSchedule()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>()).Returns((WorkSchedule?)null);

        var act = () => Sut().SetAsync(
            new SetScheduleCommand(1001, new(18, 0), new(9, 0), WorkDays.Weekdays, FreeSchedule: false));

        await act.Should().ThrowAsync<InvalidScheduleException>();
    }

    [Fact]
    public async Task Get_NoSchedule_ThrowsScheduleNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>()).Returns((WorkSchedule?)null);

        var act = () => Sut().GetAsync(1001);

        await act.Should().ThrowAsync<ScheduleNotFoundException>();
    }

    [Fact]
    public async Task Get_ExistingSchedule_Returns()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _schedules.FindByUserAsync(1001, Arg.Any<CancellationToken>())
            .Returns(new WorkScheduleBuilder().ForUser(1001).Build());

        var result = await Sut().GetAsync(1001);

        result.UserId.Should().Be(1001);
    }
}
