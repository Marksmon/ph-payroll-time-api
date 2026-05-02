using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        int status;
        string type, title, detail;
        IDictionary<string, object?>? extensions;

        switch (exception)
        {
            case EntityNotFoundException ex:
                (status, type, title, detail, extensions) = (404, ProblemTypes.NotFound, "Not Found", ex.Message, null);
                break;
            case DomainValidationException ex:
                (status, type, title, detail, extensions) = (400, ProblemTypes.Validation, "Validation Error", ex.Message, null);
                break;
            case ScheduleOverlapException ex:
                (status, type, title, detail, extensions) = (409, ProblemTypes.OverlappingSchedule, "Schedule Conflict", ex.Message, null);
                break;
            case StaleApprovalException ex:
                (status, type, title, detail, extensions) = (409, ProblemTypes.StaleApproval, "Stale Approval", ex.Message, null);
                break;
            case ConflictException ex:
                (status, type, title, detail, extensions) = (409, ProblemTypes.Conflict, "Conflict", ex.Message, null);
                break;
            case UnprocessableEntityException ex:
                (status, type, title, detail, extensions) = (422, ProblemTypes.UnprocessableEntity, "Unprocessable Entity", ex.Message, null);
                break;
            case ComputationInvariantException ex:
                (status, type, title, detail, extensions) = (422, ProblemTypes.ComputationInvariant, "Computation Invariant Violated", ex.Message,
                    new Dictionary<string, object?> { ["violations"] = ex.Violations });
                break;
            default:
                (status, type, title, detail, extensions) = (500, ProblemTypes.InternalError, "An unexpected error occurred", "Please try again later.", null);
                break;
        }

        if (status == 500)
            _logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = status;

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

        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
