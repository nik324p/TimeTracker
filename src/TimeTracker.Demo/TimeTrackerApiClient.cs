using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TimeTracker.Demo;

/// <summary>
/// Typed HTTP client for TimeTracker.Api. Every endpoint is POST + application/json (per the spec),
/// including the reads. No call throws on a non-success status: failures are read as ProblemDetails
/// and returned as a failing <see cref="ApiResult{T}"/> carrying the API's title/detail. The
/// double-tap 409 is just one instance of that path — the UI renders whatever the API said.
/// </summary>
public sealed class TimeTrackerApiClient(HttpClient http, JsonSerializerOptions json)
{
    // ---- Cards ---------------------------------------------------------
    public Task<ApiResult<CardResponse>> AssignCardAsync(AssignCardRequest request, CancellationToken ct = default) =>
        PostAsync<AssignCardRequest, CardResponse>("/card/assign", request, ct);

    // /card/touch — arrival/departure inferred from same-day state; a third tap → 409.
    public Task<ApiResult<CardResponse>> TouchCardAsync(TouchRequest request, CancellationToken ct = default) =>
        PostAsync<TouchRequest, CardResponse>("/card/touch", request, ct);

    public Task<ApiResult<CardResponse>> DeleteCardAsync(DeleteCardRequest request, CancellationToken ct = default) =>
        PostAsync<DeleteCardRequest, CardResponse>("/card/delete", request, ct);

    public Task<ApiResult<CardListResponse>> ListCardsByUserAsync(UserIdRequest request, CancellationToken ct = default) =>
        PostAsync<UserIdRequest, CardListResponse>("/card/list_by_user", request, ct);

    public Task<ApiResult<CardListResponse>> DeleteAllCardsByUserAsync(UserIdRequest request, CancellationToken ct = default) =>
        PostAsync<UserIdRequest, CardListResponse>("/card/delete_all_by_user", request, ct);

    // ---- Dev seeding (env-gated /dev/*, NON-SPEC) ----------------------
    // A 404 here just means seeding is disabled (gate off); callers fall back to live taps.
    public Task<ApiResult<CreateUserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default) =>
        PostAsync<CreateUserRequest, CreateUserResponse>("/dev/create_user", request, ct);

    public Task<ApiResult<CardResponse>> SeedTapAsync(SeedTapRequest request, CancellationToken ct = default) =>
        PostAsync<SeedTapRequest, CardResponse>("/dev/seed_tap", request, ct);

    // ---- Work time: schedule -------------------------------------------
    public Task<ApiResult<WorkTimeResponse>> SetWorkTimeAsync(SetWorkTimeRequest request, CancellationToken ct = default) =>
        PostAsync<SetWorkTimeRequest, WorkTimeResponse>("/work_time/set", request, ct);

    public Task<ApiResult<WorkTimeResponse>> GetWorkTimeAsync(UserIdRequest request, CancellationToken ct = default) =>
        PostAsync<UserIdRequest, WorkTimeResponse>("/work_time/get", request, ct);

    // ---- Work time: exclusions -----------------------------------------
    public Task<ApiResult<ExclusionResponse>> AddExclusionAsync(AddExclusionRequest request, CancellationToken ct = default) =>
        PostAsync<AddExclusionRequest, ExclusionResponse>("/work_time/add_exclusion", request, ct);

    public Task<ApiResult<ExclusionsResponse>> GetExclusionsAsync(UserIdRequest request, CancellationToken ct = default) =>
        PostAsync<UserIdRequest, ExclusionsResponse>("/work_time/get_exclusion", request, ct);

    // ---- Work time: history --------------------------------------------
    public Task<ApiResult<HistoryResponse>> GetHistoryByUserAsync(HistoryByUserRequest request, CancellationToken ct = default) =>
        PostAsync<HistoryByUserRequest, HistoryResponse>("/work_time/history_by_user", request, ct);

    public Task<ApiResult<HistoryResponse>> GetHistoryAsync(HistorySummaryRequest request, CancellationToken ct = default) =>
        PostAsync<HistorySummaryRequest, HistoryResponse>("/work_time/history", request, ct);

    // ---- Work time: statistics -----------------------------------------
    public Task<ApiResult<UserStatisticsResponse>> GetStatisticsByUserAsync(StatisticsByUserRequest request, CancellationToken ct = default) =>
        PostAsync<StatisticsByUserRequest, UserStatisticsResponse>("/work_time/statistics_by_user", request, ct);

    public Task<ApiResult<StatisticsSummaryResponse>> GetStatisticsAsync(StatisticsSummaryRequest request, CancellationToken ct = default) =>
        PostAsync<StatisticsSummaryRequest, StatisticsSummaryResponse>("/work_time/statistics", request, ct);

    // ---- shared POST helper: success → typed value; failure → message --
    private async Task<ApiResult<TRes>> PostAsync<TReq, TRes>(string path, TReq request, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(path, request, json, ct);
        }
        catch (HttpRequestException ex)
        {
            // Connection refused / DNS / TLS — surface a readable message instead of throwing into the UI.
            return ApiResult<TRes>.Fail($"Could not reach the API: {ex.Message}", 0);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadFromJsonAsync<TRes>(json, ct);
                return value is null
                    ? ApiResult<TRes>.Fail("Empty response body.", (int)response.StatusCode)
                    : ApiResult<TRes>.Ok(value);
            }

            var message = await ReadProblemMessageAsync(response, ct);
            return ApiResult<TRes>.Fail(message, (int)response.StatusCode);
        }
    }

    private async Task<string> ReadProblemMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(json, ct);
            var msg = problem?.Detail ?? problem?.Title;
            if (!string.IsNullOrWhiteSpace(msg))
            {
                return msg!;
            }
        }
        catch
        {
            // Body wasn't problem+json — fall through to a generic message.
        }

        return response.StatusCode == HttpStatusCode.Conflict
            ? "Conflict (409): the card was already tapped for this intent today."
            : $"Request failed with status {(int)response.StatusCode}.";
    }
}
