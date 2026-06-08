namespace TimeTracker.Tests;

public sealed class CardServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.AtNineAm();

    private CardService Sut() => new(_users, _cards, _uow, _clock);

    [Fact]
    public async Task Assign_UserMissing_ThrowsUserNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().AssignAsync(1001, "CARD-1");

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Assign_CardAlreadyAssigned_ThrowsCardAlreadyAssigned()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _cards.FindByUidAsync("CARD-1", Arg.Any<CancellationToken>())
            .Returns(new CardBuilder().WithUid("CARD-1").AssignedTo(7).Build());

        var act = () => Sut().AssignAsync(1001, "CARD-1");

        await act.Should().ThrowAsync<CardAlreadyAssignedException>();
    }

    [Fact]
    public async Task Assign_NewCard_AddsAndSaves()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _cards.FindByUidAsync("CARD-1", Arg.Any<CancellationToken>()).Returns((Card?)null);

        var result = await Sut().AssignAsync(1001, "CARD-1");

        result.CardUid.Should().Be("CARD-1");
        result.UserId.Should().Be(1001);
        _cards.Received(1).Add(Arg.Is<Card>(c => c.Uid == "CARD-1" && c.UserId == 1001));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_UnknownCard_ThrowsCardNotFound()
    {
        _cards.FindByUidAsync("CARD-1", Arg.Any<CancellationToken>()).Returns((Card?)null);

        var act = () => Sut().DeleteAsync("CARD-1");

        await act.Should().ThrowAsync<CardNotFoundException>();
    }

    [Fact]
    public async Task Delete_ExistingCard_RemovesAndReturnsOwner()
    {
        var card = new CardBuilder().WithUid("CARD-1").AssignedTo(1001).Build();
        _cards.FindByUidAsync("CARD-1", Arg.Any<CancellationToken>()).Returns(card);

        var result = await Sut().DeleteAsync("CARD-1");

        result.Should().Be(new CardAssignment("CARD-1", 1001));
        _cards.Received(1).Remove(card);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListByUser_UserMissing_ThrowsUserNotFound()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Sut().ListByUserAsync(1001);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task ListByUser_ReturnsCardUids()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _cards.ListByUserAsync(1001, Arg.Any<CancellationToken>()).Returns(
        [
            new CardBuilder().WithUid("A").AssignedTo(1001).Build(),
            new CardBuilder().WithUid("B").AssignedTo(1001).Build(),
        ]);

        var result = await Sut().ListByUserAsync(1001);

        result.Should().Equal("A", "B");
    }

    [Fact]
    public async Task DeleteAllByUser_RemovesAllAndReturnsUids()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        var cards = new[]
        {
            new CardBuilder().WithUid("A").AssignedTo(1001).Build(),
            new CardBuilder().WithUid("B").AssignedTo(1001).Build(),
        };
        _cards.ListByUserAsync(1001, Arg.Any<CancellationToken>()).Returns(cards);

        var result = await Sut().DeleteAllByUserAsync(1001);

        result.Should().Equal("A", "B");
        _cards.Received(1).RemoveRange(Arg.Any<IEnumerable<Card>>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAllByUser_NoCards_ReturnsEmpty_DoesNotSave()
    {
        _users.ExistsAsync(1001, Arg.Any<CancellationToken>()).Returns(true);
        _cards.ListByUserAsync(1001, Arg.Any<CancellationToken>()).Returns([]);

        var result = await Sut().DeleteAllByUserAsync(1001);

        result.Should().BeEmpty();
        _cards.DidNotReceive().RemoveRange(Arg.Any<IEnumerable<Card>>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
