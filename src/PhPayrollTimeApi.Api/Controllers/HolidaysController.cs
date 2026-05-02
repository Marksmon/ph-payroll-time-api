using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Commands.Holidays;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Application.Queries.Holidays;
using PhPayrollTimeApi.Api.Constants;
using PhPayrollTimeApi.Api.Models.Requests;

namespace PhPayrollTimeApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/holidays")]
[Authorize]
[EnableRateLimiting("standard")]
public class HolidaysController : ControllerBase
{
    private readonly ICommandHandler<CreateHolidayCommand> _create;
    private readonly ICommandHandler<UpdateHolidayCommand> _update;
    private readonly ICommandHandler<DeleteHolidayCommand> _delete;
    private readonly IQueryHandler<GetHolidaysQuery, List<HolidayCalendarEntryDto>> _get;

    public HolidaysController(
        ICommandHandler<CreateHolidayCommand> create,
        ICommandHandler<UpdateHolidayCommand> update,
        ICommandHandler<DeleteHolidayCommand> delete,
        IQueryHandler<GetHolidaysQuery, List<HolidayCalendarEntryDto>> get)
    {
        _create = create;
        _update = update;
        _delete = delete;
        _get = get;
    }

    [HttpPost]
    [Authorize(Roles = "HR_ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest req, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await _create.HandleAsync(new CreateHolidayCommand(id, req.Date, req.Name, req.Type), ct);
        return Created($"/api/v1/holidays/{req.Date:yyyy-MM-dd}", new { id, req.Date, req.Name, type = req.Type.ToString() });
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken ct)
    {
        if (startDate > endDate)
            return new ContentResult
            {
                StatusCode = 400,
                ContentType = "application/problem+json",
                Content = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = ProblemTypes.Validation,
                    title = "Bad Request",
                    status = 400,
                    detail = "startDate must be before or equal to endDate."
                })
            };

        var dtos = await _get.HandleAsync(new GetHolidaysQuery(startDate, endDate), ct);
        return Ok(dtos);
    }

    [HttpPut("{date}")]
    [Authorize(Roles = "HR_ADMIN")]
    public async Task<IActionResult> Update(DateOnly date, [FromBody] UpdateHolidayRequest req, CancellationToken ct)
    {
        await _update.HandleAsync(new UpdateHolidayCommand(date, req.Name, req.Type), ct);
        return Ok(new { date, req.Name, type = req.Type.ToString() });
    }

    [HttpDelete("{date}")]
    [Authorize(Roles = "HR_ADMIN")]
    public async Task<IActionResult> Delete(DateOnly date, CancellationToken ct)
    {
        await _delete.HandleAsync(new DeleteHolidayCommand(date), ct);
        return NoContent();
    }
}
