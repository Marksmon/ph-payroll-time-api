using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhPayrollTimeApi.Domain.Exceptions;

namespace PhPayrollTimeApi.Integration.Tests.Infrastructure;

[ApiController]
[Route("api/v1/test/probe")]
[AllowAnonymous]
public class ErrorProbeController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() =>
        throw new EntityNotFoundException("Test entity not found");

    [HttpGet("validation")]
    public IActionResult ThrowValidation() =>
        throw new DomainValidationException("Test validation failure");

    [HttpGet("overlap")]
    public IActionResult ThrowOverlap() =>
        throw new ScheduleOverlapException("Test schedule overlap");

    [HttpGet("stale")]
    public IActionResult ThrowStale() =>
        throw new StaleApprovalException("Test stale approval");

    [HttpGet("invariant")]
    public IActionResult ThrowInvariant() =>
        throw new ComputationInvariantException(new[] { "Invariant 2: overlapping segments" });

    [HttpGet("unhandled")]
    public IActionResult ThrowUnhandled() =>
        throw new InvalidOperationException("Test unhandled exception — should not leak to response");

    [HttpPost("model-validation")]
    public IActionResult TestModelValidation([FromBody] ModelValidationRequest request) =>
        Ok(request);
}

public record ModelValidationRequest([Required] string RequiredField);
