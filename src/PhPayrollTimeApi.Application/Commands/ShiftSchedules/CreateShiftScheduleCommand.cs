using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.ShiftSchedules;

public record CreateShiftScheduleCommand(
    Guid Id,
    Guid EmployeeId,
    DateTimeOffset ScheduleStart,
    DateTimeOffset ScheduleEnd,
    IReadOnlyList<BreakWindowDto> BreakWindows);

public class CreateShiftScheduleCommandHandler : ICommandHandler<CreateShiftScheduleCommand>
{
    private readonly IShiftScheduleRepository _scheduleRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public CreateShiftScheduleCommandHandler(
        IShiftScheduleRepository scheduleRepo,
        IEmployeeRepository employeeRepo)
    {
        _scheduleRepo = scheduleRepo;
        _employeeRepo = employeeRepo;
    }

    public async Task HandleAsync(CreateShiftScheduleCommand cmd, CancellationToken ct)
    {
        if (await _employeeRepo.GetByIdAsync(cmd.EmployeeId, ct) is null)
            throw new EntityNotFoundException("Employee", cmd.EmployeeId);

        if (cmd.ScheduleEnd <= cmd.ScheduleStart)
            throw new DomainValidationException("ScheduleEnd must be after ScheduleStart.");

        if (await _scheduleRepo.HasOverlapAsync(cmd.EmployeeId, cmd.ScheduleStart, cmd.ScheduleEnd, null, ct))
            throw new ScheduleOverlapException(
                $"The requested schedule overlaps with an existing schedule for employee {cmd.EmployeeId}.");

        var now = DateTimeOffset.UtcNow;
        await _scheduleRepo.AddAsync(new ShiftSchedule
        {
            Id = cmd.Id,
            EmployeeId = cmd.EmployeeId,
            ScheduleStart = cmd.ScheduleStart,
            ScheduleEnd = cmd.ScheduleEnd,
            BreakWindows = cmd.BreakWindows
                .Select(b => new BreakWindow { BreakStart = b.BreakStart, BreakEnd = b.BreakEnd })
                .ToList(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        }, ct);

        await _scheduleRepo.SaveAsync(ct);
    }
}
