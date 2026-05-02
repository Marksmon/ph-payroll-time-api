using Microsoft.AspNetCore.Diagnostics;
using PhPayrollTimeApi.Api.Constants;
using PhPayrollTimeApi.Domain.Exceptions;

namespace PhPayrollTimeApi.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, type, title, detail, extensions) = exception switch
        {
            EntityNotFoundException ex =>
                (404, ProblemTypes.NotFound, "Not Found", ex.Message, (IDictionary<string, object?>?)null),
            DomainValidationException ex =>
                (400, ProblemTypes.Validation, "Validation Error", ex.Message, null),
            ScheduleOverlapException ex =>
                (409, ProblemTypes.ConflictOverlappingSchedule, "Schedule Conflict", ex.Message, null),
            StaleApprovalException ex =>
                (409, ProblemTypes.ConflictStaleApproval, "Stale Approval", ex.Message, null),
            ComputationInvariantException ex =>
                (422, ProblemTypes.ComputationInvariant, "Computation Invariant Violated", ex.Message,
                    (IDictionary<string, object?>)new Dictionary<string, object?> { ["violations"] = ex.Violations }),
            _ =>
                (500, ProblemTypes.InternalError, "An unexpected error occurred", "Please try again later.", null)
        };

        if (status == 500)
            _logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail
        };

        if (extensions is not null)
            foreach (var (key, value) in extensions)
                problem.Extensions[key] = value;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
