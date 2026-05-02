using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IWorkSchedulePatternRepository
{
    Task<WorkSchedulePattern?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<WorkSchedulePattern>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct);
    Task AddAsync(WorkSchedulePattern pattern, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<WorkSchedulePattern> patterns, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
