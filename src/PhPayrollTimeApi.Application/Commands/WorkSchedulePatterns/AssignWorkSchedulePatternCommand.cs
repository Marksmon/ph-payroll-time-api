using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.WorkSchedulePatterns;

public record AssignWorkSchedulePatternCommand(
    Guid Id,
    Guid EmployeeId,
    IReadOnlyList<int> RestDays,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate);

public class AssignWorkSchedulePatternCommandHandler : ICommandHandler<AssignWorkSchedulePatternCommand>
{
    private readonly IWorkSchedulePatternRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public AssignWorkSchedulePatternCommandHandler(
        IWorkSchedulePatternRepository repo,
        IEmployeeRepository employeeRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
    }

    public async Task HandleAsync(AssignWorkSchedulePatternCommand cmd, CancellationToken ct)
    {
        if (await _employeeRepo.GetByIdAsync(cmd.EmployeeId, ct) is null)
            throw new EntityNotFoundException("Employee", cmd.EmployeeId);

        if (cmd.ExpiryDate.HasValue && cmd.ExpiryDate.Value <= cmd.EffectiveDate)
            throw new DomainValidationException("ExpiryDate must be after EffectiveDate.");

        var now = DateTimeOffset.UtcNow;
        await _repo.AddAsync(new WorkSchedulePattern
        {
            Id = cmd.Id,
            EmployeeId = cmd.EmployeeId,
            RestDays = cmd.RestDays.Select(d => (DayOfWeek)d).ToList(),
            EffectiveDate = cmd.EffectiveDate,
            ExpiryDate = cmd.ExpiryDate,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        }, ct);

        await _repo.SaveAsync(ct);
    }
}
