namespace TimeTracker.Core;

/// <summary>Lateness / early-departure tolerance. Bound from the <c>Lateness</c> config section by the Api.</summary>
public sealed class LatenessOptions
{
    public const string SectionName = "Lateness";

    /// <summary>Minutes of tolerance before an arrival is "late" / a departure is "left early".</summary>
    public int GraceMinutes { get; set; } = 5;

    /// <summary>The grace tolerance as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan Grace => TimeSpan.FromMinutes(GraceMinutes);
}
