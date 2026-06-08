namespace TimeTracker.Core;

/// <summary>
/// Normalizes NFC card UIDs to a canonical form so all comparisons (lookup, uniqueness) are
/// consistent across layers. Trims surrounding whitespace and upper-cases (invariant).
/// </summary>
public static class CardUidNormalizer
{
    /// <summary>Returns the canonical form of <paramref name="raw"/> (trimmed, upper-invariant).</summary>
    public static string Normalize(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return raw.Trim().ToUpperInvariant();
    }
}
