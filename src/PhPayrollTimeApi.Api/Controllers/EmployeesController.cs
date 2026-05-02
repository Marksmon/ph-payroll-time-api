using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Commands.Employees;
using PhPayrollTimeApi.Application.Commands.ShiftSchedules;
using PhPayrollTimeApi.Application.Commands.TimeLogs;
using PhPayrollTimeApi.Application.Commands.WorkSchedulePatterns;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Application.Queries.Employees;
using PhPayrollTimeApi.Application.Queries.ShiftSchedules;
using PhPayrollTimeApi.Application.Queries.TimeLogs;
using PhPayrollTimeApi.Application.Queries.WorkSchedulePatterns;
using PhPayrollTimeApi.Api.Constants;
using PhPayrollTimeApi.Api.Models.Requests;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
[EnableRateLimiting("standard")]
public class EmployeesController : ControllerBase
{
    private readonly IQueryHandler<GetEmployeeQuery, EmployeeDto> _getEmployee;
    private readonly ICommandHandler<CreateEmployeeCommand> _createEmployee;
    private readonly ICommandHandler<UpdateEmployeeCommand> _updateEmployee;
    private readonly ICommandHandler<DeactivateEmployeeCommand> _deactivateEmployee;
    private readonly IQueryHandler<GetShiftSchedulesQuery, List<ShiftScheduleDto>> _getSchedules;
    private readonly ICommandHandler<CreateShiftScheduleCommand> _createSchedule;
    private readonly ICommandHandler<UpdateShiftScheduleCommand> _updateSchedule;
    private readonly ICommandHandler<DeleteShiftScheduleCommand> _deleteSchedule;
    private readonly IQueryHandler<GetWorkSchedulePatternsQuery, List<WorkSchedulePatternDto>> _getPatterns;
    private readonly ICommandHandler<AssignWorkSchedulePatternCommand> _assignPattern;
    private readonly ICommandHandler<BulkAssignWorkSchedulePatternCommand> _bulkAssignPattern;
    private readonly ICommandHandler<UpdateWorkSchedulePatternCommand> _updatePattern;
    private readonly IQueryHandler<GetTimeLogsQuery, List<TimeLogDto>> _getTimeLogs;
    private readonly ICommandHandler<SubmitTimeLogCommand> _submitTimeLog;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IClockProvider _clock;

    public EmployeesController(
        IQueryHandler<GetEmployeeQuery, EmployeeDto> getEmployee,
        ICommandHandler<CreateEmployeeCommand> createEmployee,
        ICommandHandler<UpdateEmployeeCommand> updateEmployee,
        ICommandHandler<DeactivateEmployeeCommand> deactivateEmployee,
        IQueryHandler<GetShiftSchedulesQuery, List<ShiftScheduleDto>> getSchedules,
        ICommandHandler<CreateShiftScheduleCommand> createSchedule,
        ICommandHandler<UpdateShiftScheduleCommand> updateSchedule,
        ICommandHandler<DeleteShiftScheduleCommand> deleteSchedule,
        IQueryHandler<GetWorkSchedulePatternsQuery, List<WorkSchedulePatternDto>> getPatterns,
        ICommandHandler<AssignWorkSchedulePatternCommand> assignPattern,
        ICommandHandler<BulkAssignWorkSchedulePatternCommand> bulkAssignPattern,
        ICommandHandler<UpdateWorkSchedulePatternCommand> updatePattern,
        IQueryHandler<GetTimeLogsQuery, List<TimeLogDto>> getTimeLogs,
        ICommandHandler<SubmitTimeLogCommand> submitTimeLog,
        IEmployeeRepository employeeRepo,
        IClockProvider clock)
    {
        _getEmployee = getEmployee;
        _createEmployee = createEmployee;
        _updateEmployee = updateEmployee;
        _deactivateEmployee = deactivateEmployee;
        _getSchedules = getSchedules;
        _createSchedule = createSchedule;
        _updateSchedule = updateSchedule;
        _deleteSchedule = deleteSchedule;
        _getPatterns = getPatterns;
        _assignPattern = assignPattern;
        _bulkAssignPattern = bulkAssignPattern;
        _updatePattern = updatePattern;
        _getTimeLogs = getTimeLogs;
        _submitTimeLog = submitTimeLog;
        _employeeRepo = employeeRepo;
        _clock = clock;
    }

    // ── Employee CRUD ─────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "MANAGER,HR_ADMIN")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest req, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await _createEmployee.HandleAsync(new CreateEmployeeCommand(
            id, req.EmployeeNumber, req.FullName, req.Role, req.JwtSubjectClaim), ct);

        var dto = await _getEmployee.HandleAsync(new GetEmployeeQuery(id), ct);
        return CreatedAtAction(nameof(GetEmployee), new { id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
    {
        var dto = await _getEmployee.HandleAsync(new GetEmployeeQuery(id), ct);
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR_ADMIN")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest req, CancellationToken ct)
    {
        await _updateEmployee.HandleAsync(new UpdateEmployeeCommand(
            id, req.FullName, req.EmployeeNumber, req.JwtSubjectClaim), ct);

        var dto = await _getEmployee.HandleAsync(new GetEmployeeQuery(id), ct);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HR_ADMIN")]
    public async Task<IActionResult> DeactivateEmployee(Guid id, CancellationToken ct)
    {
        await _deactivateEmployee.HandleAsync(new DeactivateEmployeeCommand(id), ct);
        return NoContent();
    }

    // ── Shift Schedules ───────────────────────────────────────────────────────

    [HttpPost("{id:guid}/schedules")]
    [Authorize(Roles = "MANAGER,HR_ADMIN")]
    public async Task<IActionResult> CreateSchedule(Guid id, [FromBody] CreateShiftScheduleRequest req, CancellationToken ct)
    {
        var scheduleId = Guid.NewGuid();
        var breakWindows = req.BreakWindows?
            .Select(b => new Application.Dtos.BreakWindowDto(b.BreakStart, b.BreakEnd))
            .ToList()
            ?? new List<Application.Dtos.BreakWindowDto>();

        await _createSchedule.HandleAsync(new CreateShiftScheduleCommand(
            scheduleId, id, req.ScheduleStart, req.ScheduleEnd, breakWindows), ct);

        var schedules = await _getSchedules.HandleAsync(new GetShiftSchedulesQuery(id), ct);
        var created = schedules.First(s => s.Id == scheduleId);
        return Created($"/api/v1/employees/{id}/schedules/{scheduleId}", created);
    }

    [HttpGet("{id:guid}/schedules")]
    public async Task<IActionResult> GetSchedules(Guid id, CancellationToken ct)
    {
        if (!await CanAccessEmployeeData(id, ct))
            return ForbidProblem();

        var dtos = await _getSchedules.HandleAsync(new GetShiftSchedulesQuery(id), ct);
        return Ok(dtos);
    }

    [HttpPut("{id:guid}/schedules/{scheduleId:guid}")]
    [Authorize(Roles = "MANAGER,HR_ADMIN")]
    public async Task<IActionResult> UpdateSchedule(Guid id, Guid scheduleId,
        [FromBody] UpdateShiftScheduleRequest req, CancellationToken ct)
    {
        var breakWindows = req.BreakWindows?
            .Select(b => new Application.Dtos.BreakWindowDto(b.BreakStart, b.BreakEnd))
            .ToList()
            ?? new List<Application.Dtos.BreakWindowDto>();

        await _updateSchedule.HandleAsync(new UpdateShiftScheduleCommand(
            scheduleId, id, req.ScheduleStart, req.ScheduleEnd, breakWindows), ct);

        var schedules = await _getSchedules.HandleAsync(new GetShiftSchedulesQuery(id), ct);
        var updated = schedules.First(s => s.Id == scheduleId);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}/schedules/{scheduleId:guid}")]
    [Authorize(Roles = "MANAGER,HR_ADMIN")]
    public async Task<IActionResult> DeleteSchedule(Guid id, Guid scheduleId, CancellationToken ct)
    {
        await _deleteSchedule.HandleAsync(new DeleteShiftScheduleCommand(scheduleId, id), ct);
        return NoContent();
    }

    // ── Work Schedule Patterns ────────────────────────────────────────────────

    [HttpPost("{id:guid}/schedule-patterns")]
    [Authorize(Roles = "MANAGER,HR_ADMIN")]
    public async Task<IActionResult> AssignPattern(Guid id, [FromBody] AssignWorkSchedulePatternRequest req, CancellationToken ct)
    {
        var patternId = Guid.NewGuid();
        await _assignPattern.HandleAsync(new AssignWorkSchedulePatternCommand(
            patternId, id, req.RestDays, req.EffectiveDate, req.ExpiryDate), ct);

        var patterns = await _getPatterns.HandleAsync(new GetWorkSchedulePatternsQuery(id), ct);
        var created = patterns.First(p => p.Id == patternId);
        return Created($"/api/v1/employees/{id}/schedule-patterns/{patternId}", created);
    }

    [HttpGet("{id:guid}/schedule-patterns")]
    public async Task<IActionResult> GetPatterns(Guid id, CancellationToken ct)
    {
        if (!await CanAccessEmployeeData(id, ct))
            return ForbidProblem();

        var dtos = await _getPatterns.HandleAsync(new GetWorkSchedulePatternsQuery(id), ct);
        return Ok(dtos);
    }

    [HttpPut("{id:guid}/schedule-patterns/{patternId:guid}")]
    [Authorize(Roles = "MANAGER,HR_ADMIN")]
    public async Task<IActionResult> UpdatePattern(Guid id, Guid patternId,
        [FromBody] UpdateWorkSchedulePatternRequest req, CancellationToken ct)
    {
        await _updatePattern.HandleAsync(new UpdateWorkSchedulePatternCommand(
            patternId, id, req.EffectiveDate, req.ExpiryDate), ct);

        var patterns = await _getPatterns.HandleAsync(new GetWorkSchedulePatternsQuery(id), ct);
        var updated = patterns.First(p => p.Id == patternId);
        return Ok(updated);
    }

    [HttpPost("schedule-patterns/bulk")]
    [Authorize(Roles = "MANAGER,HR_ADMIN")]
    [EnableRateLimiting("bulk")]
    public async Task<IActionResult> BulkAssignPatterns(
        [FromBody] BulkAssignWorkSchedulePatternRequest req, CancellationToken ct)
    {
        await _bulkAssignPattern.HandleAsync(new BulkAssignWorkSchedulePatternCommand(
            req.EmployeeIds, req.RestDays, req.EffectiveDate, req.ExpiryDate), ct);
        return Ok(new { count = req.EmployeeIds.Count });
    }

    // ── Time Logs ─────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/logs")]
    public async Task<IActionResult> SubmitTimeLog(Guid id, [FromBody] SubmitTimeLogRequest req, CancellationToken ct)
    {
        var callerRole = User.FindFirstValue("role");
        var callerSub = User.FindFirstValue("sub");

        if (callerRole == "EMPLOYEE")
        {
            var emp = await _employeeRepo.GetByIdAsync(id, ct);
            if (emp is null || emp.JwtSubjectClaim != callerSub)
                return ForbidProblem();
        }

        var loggedAt = req.LoggedAt ?? _clock.UtcNow;
        var logId = Guid.NewGuid();

        await _submitTimeLog.HandleAsync(new SubmitTimeLogCommand(
            logId, id, req.LogType, loggedAt, req.Source), ct);

        var logs = await _getTimeLogs.HandleAsync(
            new GetTimeLogsQuery(id, loggedAt.AddMinutes(-1), loggedAt.AddMinutes(1)), ct);
        var created = logs.FirstOrDefault(l => l.Id == logId);

        return Created($"/api/v1/employees/{id}/logs/{logId}", created);
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<IActionResult> GetTimeLogs(
        Guid id,
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate,
        CancellationToken ct)
    {
        if (startDate > endDate)
            return BadRequestProblem("startDate must be before or equal to endDate.");

        if (!await CanAccessEmployeeData(id, ct))
            return ForbidProblem();

        var dtos = await _getTimeLogs.HandleAsync(new GetTimeLogsQuery(id, startDate, endDate), ct);
        return Ok(dtos);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> CanAccessEmployeeData(Guid employeeId, CancellationToken ct)
    {
        var callerRole = User.FindFirstValue("role");
        if (callerRole is "MANAGER" or "HR_ADMIN")
            return true;

        var callerSub = User.FindFirstValue("sub");
        var emp = await _employeeRepo.GetByIdAsync(employeeId, ct);
        return emp is not null && emp.JwtSubjectClaim == callerSub;
    }

    private IActionResult ForbidProblem() =>
        new ContentResult
        {
            StatusCode = 403,
            ContentType = "application/problem+json",
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = ProblemTypes.Forbidden,
                title = "Forbidden",
                status = 403,
                detail = "You are not authorized to access this resource."
            })
        };

    private IActionResult BadRequestProblem(string detail) =>
        new ContentResult
        {
            StatusCode = 400,
            ContentType = "application/problem+json",
            Content = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = ProblemTypes.Validation,
                title = "Bad Request",
                status = 400,
                detail
            })
        };
}
