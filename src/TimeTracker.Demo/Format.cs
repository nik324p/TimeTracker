namespace TimeTracker.Demo;

/// <summary>Display helpers shared by the components (the API speaks minutes; humans read hours).</summary>
public static class Format
{
    /// <summary>e.g. 450 → "7h 30m"; null → "—".</summary>
    public static string Hours(long? minutes)
    {
        if (minutes is null)
        {
            return "—";
        }

        var sign = minutes < 0 ? "-" : string.Empty;
        var abs = Math.Abs(minutes.Value);
        return $"{sign}{abs / 60}h {abs % 60:00}m";
    }

    /// <summary>Local "HH:mm" for an instant, or "—".</summary>
    public static string Time(DateTimeOffset? at) => at?.ToLocalTime().ToString("HH:mm") ?? "—";
}
