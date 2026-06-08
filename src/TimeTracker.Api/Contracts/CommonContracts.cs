using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Api;

// Shared by list_by_user, delete_all_by_user, work_time/get, get_exclusion.
public sealed record UserIdRequest(
    [property: Range(1, long.MaxValue)] long UserId);

/// <summary>Maps Core domain results to the wire DTOs. Split partial — one part per category.</summary>
internal static partial class ResponseMappings
{
    public static long ToMinutes(this TimeSpan duration) => (long)duration.TotalMinutes;
}
