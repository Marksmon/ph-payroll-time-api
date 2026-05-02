using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.ShiftSchedules;

public record UpdateShiftScheduleCommand(
    Guid ScheduleId,
    Guid EmployeeId,
    DateTimeOffset ScheduleStart,
    DateTimeOffset ScheduleEnd,
    IReadOnlyList<BreakWindowDto> BreakWindows);

public class UpdateShiftScheduleCommandHandler : ICommandHandler<UpdateShiftScheduleCommand>
{
    private readonly IShiftScheduleRepository _repo;

    public UpdateShiftScheduleCommandHandler(IShiftScheduleRepository repo) => _repo = repo;

    public async Task HandleAsync(UpdateShiftScheduleCommand cmd, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(cmd.ScheduleId, ct)
            ?? throw new EntityNotFoundException("ShiftSchedule", cmd.ScheduleId);

        if (schedule.EmployeeId != cmd.EmployeeId)
            throw new EntityNotFoundException("ShiftSchedule", cmd.ScheduleId);

        if (cmd.ScheduleEnd <= cmd.ScheduleStart)
            throw new DomainValidationException("ScheduleEnd must be after ScheduleStart.");

        if (await _repo.HasOverlapAsync(cmd.EmployeeId, cmd.ScheduleStart, cmd.ScheduleEnd, cmd.ScheduleId, ct))
            throw new ScheduleOverlapException(
                $"The updated schedule overlaps with an existing schedule for employee {cmd.EmployeeId}.");

        schedule.ScheduleStart = cmd.ScheduleStart;
        schedule.ScheduleEnd = cmd.ScheduleEnd;
        schedule.BreakWindows = cmd.BreakWindows
            .Select(b => new BreakWindow { BreakStart = b.BreakStart, BreakEnd = b.BreakEnd })
            .ToList();
        schedule.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.SaveAsync(ct);
    }
}
