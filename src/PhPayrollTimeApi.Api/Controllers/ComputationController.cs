using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Application.Queries.GetComputation;

namespace PhPayrollTimeApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/schedules")]
[Authorize]
[EnableRateLimiting("standard")]
public class ComputationController : ControllerBase
{
    private readonly IQueryHandler<GetComputationQuery, ComputationResultDto> _getComputation;

    public ComputationController(IQueryHandler<GetComputationQuery, ComputationResultDto> getComputation)
        => _getComputation = getComputation;

    [HttpGet("{id:guid}/computation")]
    public async Task<IActionResult> GetComputation(Guid id, CancellationToken ct)
    {
        var result = await _getComputation.HandleAsync(new GetComputationQuery(id), ct);
        return Ok(result);
    }
}
