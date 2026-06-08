namespace TimeTracker.Tests;

public sealed class ExclusionServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IScheduleExclusionRepository _exclusions = Substitute.For<IScheduleExclusionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ExclusionService Sut() => new(_users, _exclusions, _uow);

    [Fact]
    public async Task Add_UserMissing_ThrowsUserNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().AddAsync(
            new AddExclusionCommand(1001, ExclusionType.ArriveLater, TestClock.On(new(9, 0)), TestClock.On(new(12, 0))));

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Add_InvalidRange_ThrowsInvalidExclusion()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => Sut().AddAsync(
            new AddExclusionCommand(1001, ExclusionType.ArriveLater, TestClock.On(new(12, 0)), TestClock.On(new(8, 0))));

        await act.Should().ThrowAsync<InvalidExclusionException>();
    }

    [Fact]
    public async Task Add_Valid_AddsAndSaves()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut().AddAsync(
            new AddExclusionCommand(1001, ExclusionType.ArriveLater, TestClock.On(new(9, 0)), TestClock.On(new(12, 0))));

        result.Type.Should().Be(ExclusionType.ArriveLater);
        _exclusions.Received(1).Add(Arg.Any<ScheduleExclusion>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByUser_UserMissing_ThrowsUserNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().GetByUserAsync(1001);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task GetByUser_ReturnsExclusions()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _exclusions.ListByUserAsync(1001, Arg.Any<CancellationToken>())
            .Returns([new ScheduleExclusionBuilder().ForUser(1001).Build()]);

        var result = await Sut().GetByUserAsync(1001);

        result.Should().HaveCount(1);
    }
}
