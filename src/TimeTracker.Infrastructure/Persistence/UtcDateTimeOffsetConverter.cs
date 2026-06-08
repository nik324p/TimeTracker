using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TimeTracker.Infrastructure;

/// <summary>
/// Normalizes every <see cref="DateTimeOffset"/> to UTC on write so it persists cleanly as
/// PostgreSQL <c>timestamptz</c>. Domain instants are already UTC; this also handles any
/// non-zero-offset value arriving from the Api (e.g. an exclusion datetime).
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(v => v.ToUniversalTime(), v => v)
    {
    }
}
