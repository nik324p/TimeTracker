namespace TimeTracker.Core;

/// <summary>
/// Canonical snake_case domain error codes — the single source shared by each
/// <see cref="DomainException.Code"/> and the Api's status mapping. Stays in Core so both
/// sides reference the same literal; the value is wire-visible (ProblemDetails <c>type</c>/<c>code</c>),
/// so renaming a member is safe but changing a string value is an API contract break.
/// </summary>
public static class ErrorCodes
{
    public const string UserNotFound        = "user_not_found";
    public const string CardNotFound        = "card_not_found";
    public const string CardAlreadyAssigned = "card_already_assigned";
    public const string AlreadyTapped       = "already_tapped";
    public const string InvalidTap          = "invalid_tap";
    public const string ScheduleNotFound    = "schedule_not_found";
    public const string InvalidSchedule     = "invalid_schedule";
    public const string InvalidExclusion    = "invalid_exclusion";
    public const string InvalidLimit        = "invalid_limit";
}
