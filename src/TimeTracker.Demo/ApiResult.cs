namespace TimeTracker.Demo;

/// <summary>
/// Outcome of an API call. Success carries a value; failure carries a human-readable message
/// (sourced from ProblemDetails.detail/title, or a status fallback) plus the status code.
/// Keeps failure handling explicit and out of try/catch at every call site.
/// </summary>
public sealed record ApiResult<T>(bool IsSuccess, T? Value, string? ErrorMessage, int? StatusCode)
{
    public static ApiResult<T> Ok(T value) => new(true, value, null, 200);

    public static ApiResult<T> Fail(string message, int statusCode) =>
        new(false, default, message, statusCode);
}
