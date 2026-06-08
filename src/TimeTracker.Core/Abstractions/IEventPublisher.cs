namespace TimeTracker.Core;

/// <summary>
/// Publishes domain events to the message broker. Declared in Core; implemented in Infrastructure
/// (raw RabbitMQ.Client). Publishing is fire-and-forget with no outbox: the impl logs and swallows
/// failures so a lost event never fails an already-committed request.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
}
