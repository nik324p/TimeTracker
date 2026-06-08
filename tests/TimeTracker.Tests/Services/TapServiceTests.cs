namespace TimeTracker.Tests;

public sealed class TapServiceTests
{
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWorkDayRepository _workDays = Substitute.For<IWorkDayRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly FakeTimeProvider _clock = TestClock.AtNineAm();

    private TapService Sut() => new(_cards, _users, _workDays, _uow, _publisher, _clock);

    private void GivenAssignedCard(string uid = "CARD-0001", long userId = 1001)
    {
        _cards.FindByUidAsync(uid, Arg.Any<CancellationToken>())
            .Returns(new CardBuilder().WithUid(uid).AssignedTo(userId).Build());
        _users.ExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task Touch_UnknownCard_ThrowsCardNotFound()
    {
        var act = () => Sut().TouchAsync("NOPE");

        await act.Should().ThrowAsync<CardNotFoundException>();
    }

    [Fact]
    public async Task Touch_CardOwnerMissing_ThrowsUserNotFound()
    {
        _cards.FindByUidAsync("CARD-0001", Arg.Any<CancellationToken>())
            .Returns(new CardBuilder().WithUid("CARD-0001").AssignedTo(1001).Build());
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().TouchAsync("CARD-0001");

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Touch_FirstTapToday_OpensWorkDay_ReturnsArrival_Publishes_Saves()
    {
        GivenAssignedCard();
        _workDays.FindByUserAndDateAsync(1001, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((WorkDay?)null);

        var result = await Sut().TouchAsync("CARD-0001");

        result.Kind.Should().Be(TapKind.Arrival);
        result.CardUid.Should().Be("CARD-0001");
        result.UserId.Should().Be(1001);
        _workDays.Received(1).Add(Arg.Any<WorkDay>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<CardTappedEvent>(e => e.Kind == TapKind.Arrival && e.UserId == 1001 && e.CardUid == "CARD-0001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Touch_SecondTapToday_RecordsDeparture()
    {
        GivenAssignedCard();
        var openDay = new WorkDayBuilder().WithArrivalAt(TestClock.On(new(8, 0))).Build();
        _workDays.FindByUserAndDateAsync(1001, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(openDay);

        var result = await Sut().TouchAsync("CARD-0001");

        result.Kind.Should().Be(TapKind.Departure);
        _workDays.DidNotReceive().Add(Arg.Any<WorkDay>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<CardTappedEvent>(e => e.Kind == TapKind.Departure), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Touch_ThirdTapToday_ThrowsAlreadyTapped_DoesNotPublishOrSave()
    {
        GivenAssignedCard();
        _workDays.FindByUserAndDateAsync(1001, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new WorkDayBuilder().WithTwoTaps().Build());

        var act = () => Sut().TouchAsync("CARD-0001");

        await act.Should().ThrowAsync<AlreadyTappedException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<CardTappedEvent>(), Arg.Any<CancellationToken>());
    }
}
