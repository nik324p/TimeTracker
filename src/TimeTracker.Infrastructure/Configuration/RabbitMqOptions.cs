namespace TimeTracker.Infrastructure;

/// <summary>RabbitMQ connection + exchange settings, bound from the <c>RabbitMq</c> configuration section.</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    /// <summary>Name of the durable topic exchange events are published to.</summary>
    public string Exchange { get; init; } = "timetracker.events";
}
