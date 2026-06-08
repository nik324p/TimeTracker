using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

/// <summary>
/// Publish-only <see cref="IEventPublisher"/> over RabbitMQ.Client v7. Holds one long-lived
/// connection, opens a channel per publish, and declares a durable topic exchange once.
/// Publishing is fire-and-forget with no outbox: broker failures are logged and swallowed so a
/// lost event never fails an already-committed request (overview.md — accepted for this project).
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private readonly RabbitMqOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqEventPublisher(
        IOptions<RabbitMqOptions> options,
        TimeProvider clock,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class
    {
        try
        {
            var connection = await EnsureConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Type = typeof(TEvent).Name,
                Timestamp = new AmqpTimestamp(_clock.GetUtcNow().ToUnixTimeSeconds()),
            };

            var body = JsonSerializer.SerializeToUtf8Bytes(@event, Json);

            await channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: RoutingKeyFor(@event),
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: the tap already committed. Log and move on (no retry, no outbox).
            _logger.LogError(
                ex,
                "Failed to publish {EventType} to exchange '{Exchange}'. Event dropped.",
                typeof(TEvent).Name,
                _options.Exchange);
        }
    }

    private async Task<IConnection> EnsureConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            // Dispose a previously-opened-but-now-closed connection before replacing it (avoid a socket leak on reconnect).
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
            };

            _connection = await factory.CreateConnectionAsync(ct);

            // Declare the exchange once per (re)connect — idempotent on the broker.
            await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync(
                exchange: _options.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string RoutingKeyFor<TEvent>(TEvent @event) => @event switch
    {
        CardTappedEvent => "card.tapped",
        _ => typeof(TEvent).Name.ToLowerInvariant(),
    };

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
