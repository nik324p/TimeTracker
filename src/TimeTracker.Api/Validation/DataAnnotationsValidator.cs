using FastEndpoints;
using FluentValidation.Results;
using DataAnnotationsValidatorType = System.ComponentModel.DataAnnotations.Validator;
using DataAnnotationsContext = System.ComponentModel.DataAnnotations.ValidationContext;
using DataAnnotationsResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace TimeTracker.Api;

/// <summary>
/// Global pre-processor that validates each request DTO's DataAnnotations attributes and short-circuits
/// with a 400 ProblemDetails on failure. This is the reliable path for FastEndpoints (the .NET 10
/// minimal-API validation interceptor does not run inside the FE pipeline); no FE FluentValidation
/// validators are registered.
/// </summary>
public sealed class DataAnnotationsValidator : IGlobalPreProcessor
{
    public async Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        if (context.Request is not { } request)
        {
            return;
        }

        var results = new List<DataAnnotationsResult>();
        var validationContext = new DataAnnotationsContext(request);
        if (DataAnnotationsValidatorType.TryValidateObject(request, validationContext, results, validateAllProperties: true))
        {
            return;
        }

        foreach (var result in results)
        {
            var property = result.MemberNames.FirstOrDefault() ?? string.Empty;
            context.ValidationFailures.Add(new ValidationFailure(property, result.ErrorMessage ?? "Invalid value."));
        }

        // Writes the configured error response (ProblemDetails) and stops the pipeline before HandleAsync.
        await context.HttpContext.Response.SendErrorsAsync(context.ValidationFailures, cancellation: ct);
    }
}
