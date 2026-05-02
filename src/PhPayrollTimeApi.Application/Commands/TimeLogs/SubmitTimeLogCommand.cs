using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.TimeLogs;

public record SubmitTimeLogCommand(
    Guid Id,
    Guid EmployeeId,
    LogType LogType,
    DateTimeOffset LoggedAt,
    string? Source);

public class SubmitTimeLogCommandHandler : ICommandHandler<SubmitTimeLogCommand>
{
    private readonly ITimeLogRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IClockProvider _clock;

    public SubmitTimeLogCommandHandler(
        ITimeLogRepository repo,
        IEmployeeRepository employeeRepo,
        IClockProvider clock)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _clock = clock;
    }

    public async Task HandleAsync(SubmitTimeLogCommand cmd, CancellationToken ct)
    {
        if (await _employeeRepo.GetByIdAsync(cmd.EmployeeId, ct) is null)
            throw new EntityNotFoundException("Employee", cmd.EmployeeId);

        if (cmd.LoggedAt > _clock.UtcNow)
            throw new UnprocessableEntityException("Timestamp cannot be in the future.");

        await _repo.AddAsync(new TimeLog
        {
            Id = cmd.Id,
            EmployeeId = cmd.EmployeeId,
            LogType = cmd.LogType,
            LoggedAt = cmd.LoggedAt,
            Source = cmd.Source,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        await _repo.SaveAsync(ct);
    }
}
