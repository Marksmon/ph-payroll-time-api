using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IShiftScheduleRepository
{
    Task<ShiftSchedule?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<ShiftSchedule>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct);
    Task<bool> HasOverlapAsync(Guid employeeId, DateTimeOffset start, DateTimeOffset end, Guid? excludeId, CancellationToken ct);
    Task AddAsync(ShiftSchedule schedule, CancellationToken ct);
    Task DeleteAsync(ShiftSchedule schedule, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
