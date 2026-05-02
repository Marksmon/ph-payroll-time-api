using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.WorkSchedulePatterns;

public record BulkAssignWorkSchedulePatternCommand(
    IReadOnlyList<Guid> EmployeeIds,
    IReadOnlyList<int> RestDays,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate);

public class BulkAssignWorkSchedulePatternCommandHandler : ICommandHandler<BulkAssignWorkSchedulePatternCommand>
{
    private readonly IWorkSchedulePatternRepository _repo;

    public BulkAssignWorkSchedulePatternCommandHandler(IWorkSchedulePatternRepository repo) => _repo = repo;

    public async Task HandleAsync(BulkAssignWorkSchedulePatternCommand cmd, CancellationToken ct)
    {
        if (cmd.EmployeeIds.Count > 100)
            throw new DomainValidationException("Bulk assignment is limited to 100 employees per request.");

        if (cmd.ExpiryDate.HasValue && cmd.ExpiryDate.Value <= cmd.EffectiveDate)
            throw new DomainValidationException("ExpiryDate must be after EffectiveDate.");

        var restDays = cmd.RestDays.Select(d => (DayOfWeek)d).ToList();
        var now = DateTimeOffset.UtcNow;

        var patterns = cmd.EmployeeIds.Select(empId => new WorkSchedulePattern
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId,
            RestDays = restDays,
            EffectiveDate = cmd.EffectiveDate,
            ExpiryDate = cmd.ExpiryDate,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _repo.AddRangeAsync(patterns, ct);
        await _repo.SaveAsync(ct);
    }
}
