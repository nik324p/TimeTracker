namespace TimeTracker.Demo;

// ─────────────────────────────────────────────────────────────────────────────
// Local mirrors of the API wire format. PascalCase here → snake_case on the wire
// via the shared SnakeCaseLower policy (Program.cs). These were reconciled
// FIELD-BY-FIELD against the real TimeTracker.Api DTOs — not the spec sketch —
// so types/tokens match exactly:
//   • days        → IReadOnlyList<DayOfWeek>, serialized as "monday".."sunday"
//   • start/end   → TimeOnly, serialized "HH:mm:ss" (e.g. "09:00:00")
//   • statistics  → MINUTES (long), plus filter/from_date/to_date echo
//   • type_exclusion / filter → plain strings carrying the exact wire tokens
// ─────────────────────────────────────────────────────────────────────────────

// ---- Shared request shapes ------------------------------------------------
public sealed record UserIdRequest(long UserId);      // user_id-only reads

// ---- Cards ----------------------------------------------------------------
public sealed record TouchRequest(string CardUid);                      // POST /card/touch
public sealed record AssignCardRequest(long UserId, string CardUid);    // POST /card/assign
public sealed record DeleteCardRequest(string CardUid);                 // POST /card/delete

// touch / assign / delete all return { card_uid, user_id }.
public sealed record CardResponse(string CardUid, long UserId);

// /card/list_by_user + /card/delete_all_by_user → { user_id, cards: [card_uid, ...] }.
public sealed record CardListResponse(long UserId, IReadOnlyList<string> Cards);

// ---- Dev seeding (env-gated /dev/*, NON-SPEC) -----------------------------
public sealed record CreateUserRequest(long UserId, string? DisplayName = null);
public sealed record CreateUserResponse(long UserId, bool Created);

// intent is accepted but ignored by the API (arrival/departure inferred from day state).
public sealed record SeedTapRequest(string CardUid, DateTimeOffset At, string? Intent = null);

// ---- Work time: schedule --------------------------------------------------
// POST /work_time/set  (user_id, start_time, end_time, days, free_schedule)
public sealed record SetWorkTimeRequest(
    long UserId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyList<DayOfWeek> Days,
    bool FreeSchedule);

// /work_time/set + /work_time/get echo the stored schedule.
public sealed record WorkTimeResponse(
    long UserId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyList<DayOfWeek> Days,
    bool FreeSchedule);

// ---- Work time: exclusions ------------------------------------------------
// type_exclusion wire tokens (spaced): "arrive later" | "leave earlier" | "full working day".
public sealed record AddExclusionRequest(
    long UserId,
    string TypeExclusion,
    DateTimeOffset StartDatetime,
    DateTimeOffset EndDatetime);

// /work_time/add_exclusion echoes the stored exclusion; /work_time/get_exclusion wraps a list.
public sealed record ExclusionResponse(
    long UserId,
    string TypeExclusion,
    DateTimeOffset StartDatetime,
    DateTimeOffset EndDatetime);

public sealed record ExclusionsResponse(long UserId, IReadOnlyList<ExclusionResponse> Exclusions);

// ---- Work time: history ---------------------------------------------------
// Both reads take an optional period filter (same tokens as statistics) — default "month".
public sealed record HistoryByUserRequest(long UserId, string Filter = "month");   // /work_time/history_by_user
public sealed record HistorySummaryRequest(int Limit, string Filter = "month");    // /work_time/history

public sealed record HistoryResponse(IReadOnlyList<HistoryEntry> Entries);

public sealed record HistoryEntry(
    long UserId,
    DateOnly Date,
    DateTimeOffset? Arrival,
    DateTimeOffset? Departure,
    long? WorkedMinutes,
    bool WasLate,
    bool LateExcused,
    bool LeftEarly,
    bool LeftEarlyExcused);

// ---- Work time: statistics ------------------------------------------------
// filter ∈ { "week", "month", "year", "entire period" } — default "month".
public sealed record StatisticsByUserRequest(long UserId, string Filter = "month");   // /work_time/statistics_by_user
public sealed record StatisticsSummaryRequest(int Limit, string Filter = "month");    // /work_time/statistics

public sealed record UserStatisticsResponse(
    long UserId,
    string Filter,
    DateOnly? FromDate,
    DateOnly ToDate,
    long RequiredMinutes,
    long WorkedMinutes,
    long UnderworkedMinutes,
    int LateWithoutReason,
    int LateWithReason,
    int LeftEarlyWithoutReason,
    int LeftEarlyWithReason);

// /work_time/statistics (all workers, limit N, period filter) → totals + per_user rows.
public sealed record StatisticsSummaryResponse(
    long TotalRequiredMinutes,
    long TotalWorkedMinutes,
    long TotalUnderworkedMinutes,
    int TotalLateWithoutReason,
    int TotalLateWithReason,
    int TotalLeftEarlyWithoutReason,
    int TotalLeftEarlyWithReason,
    IReadOnlyList<UserStatisticsResponse> PerUser);

// ---- RFC 7807 ProblemDetails (every failure path) -------------------------
public sealed record ProblemDetails(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    string? Instance);
