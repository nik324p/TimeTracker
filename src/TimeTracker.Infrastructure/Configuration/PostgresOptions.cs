namespace TimeTracker.Infrastructure;

/// <summary>PostgreSQL connection settings, bound from the <c>Postgres</c> configuration section.</summary>
public sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    public string ConnectionString { get; init; } = default!;
}
