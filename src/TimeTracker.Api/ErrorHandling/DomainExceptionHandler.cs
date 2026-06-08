using Microsoft.AspNetCore.Diagnostics;
using TimeTracker.Core;

namespace TimeTracker.Api;

/// <summary>
/// The single place domain exceptions become HTTP responses. Maps each <see cref="DomainException"/>
/// to an RFC 7807 ProblemDetails by its snake_case <see cref="DomainException.Code"/>
/// (per the Core build contract §7). Non-domain exceptions fall through to the framework (500).
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    private static readonly Dictionary<string, int> StatusByCode = new()
    {
        [ErrorCodes.UserNotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.CardNotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.ScheduleNotFound] = StatusCodes.Status404NotFound,
        [ErrorCodes.CardAlreadyAssigned] = StatusCodes.Status409Conflict,
        [ErrorCodes.AlreadyTapped] = StatusCodes.Status409Conflict,
        [ErrorCodes.InvalidTap] = StatusCodes.Status422UnprocessableEntity,
        [ErrorCodes.InvalidSchedule] = StatusCodes.Status422UnprocessableEntity,
        [ErrorCodes.InvalidExclusion] = StatusCodes.Status422UnprocessableEntity,
        [ErrorCodes.InvalidLimit] = StatusCodes.Status400BadRequest,
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domain)
        {
            return false; // let the framework produce a 500 for non-domain errors
        }

        var status = StatusByCode.GetValueOrDefault(domain.Code, StatusCodes.Status400BadRequest);
        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = status,
                Title = Humanize(domain.Code),
                Detail = domain.Message,
                Type = $"https://timetracker/errors/{domain.Code}",
                Extensions = { ["code"] = domain.Code },
            },
        });
    }

    private static string Humanize(string code)
    {
        var spaced = code.Replace('_', ' ');
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }
}
