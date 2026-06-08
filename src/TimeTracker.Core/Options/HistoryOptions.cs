namespace TimeTracker.Core;

/// <summary>Bounds for the history and statistics list endpoints.</summary>
public sealed class HistoryOptions
{
    public const string SectionName = "History";

    public int DefaultLimit { get; set; } = 50;

    /// <summary>Hard cap applied to the requested limit for <c>/history</c> and <c>/statistics</c>.</summary>
    public int MaxLimit { get; set; } = 500;
}
