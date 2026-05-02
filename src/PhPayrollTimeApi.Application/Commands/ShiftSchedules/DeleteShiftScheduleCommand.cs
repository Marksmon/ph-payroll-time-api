using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.ShiftSchedules;

public record DeleteShiftScheduleCommand(Guid ScheduleId, Guid EmployeeId);

public class DeleteShiftScheduleCommandHandler : ICommandHandler<DeleteShiftScheduleCommand>
{
    private readonly IShiftScheduleRepository _repo;

    public DeleteShiftScheduleCommandHandler(IShiftScheduleRepository repo) => _repo = repo;

    public async Task HandleAsync(DeleteShiftScheduleCommand cmd, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(cmd.ScheduleId, ct)
            ?? throw new EntityNotFoundException("ShiftSchedule", cmd.ScheduleId);

        if (schedule.EmployeeId != cmd.EmployeeId)
            throw new EntityNotFoundException("ShiftSchedule", cmd.ScheduleId);

        await _repo.DeleteAsync(schedule, ct);
        await _repo.SaveAsync(ct);
    }
}
