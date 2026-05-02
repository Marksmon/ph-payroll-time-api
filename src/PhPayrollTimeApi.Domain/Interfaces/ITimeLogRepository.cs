using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface ITimeLogRepository
{
    Task<List<TimeLog>> GetByEmployeeAndDateRangeAsync(Guid employeeId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task AddAsync(TimeLog log, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
