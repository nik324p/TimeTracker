namespace TimeTracker.Core;

/// <summary>
/// Raised when a card tap is recorded. Published via <see cref="IEventPublisher"/> after the tap
/// is committed. The event payload contract is owned by Core so the publisher impl can serialize it.
/// </summary>
public sealed record CardTappedEvent(string CardUid, long UserId, TapKind Kind, DateTimeOffset At);
