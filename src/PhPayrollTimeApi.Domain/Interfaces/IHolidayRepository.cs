using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IHolidayRepository
{
    Task<HolidayCalendarEntry?> GetByDateAsync(DateOnly date, CancellationToken ct);
    Task<HolidayCalendarEntry?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<HolidayCalendarEntry>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<bool> DateExistsAsync(DateOnly date, CancellationToken ct);
    Task AddAsync(HolidayCalendarEntry entry, CancellationToken ct);
    Task DeleteAsync(HolidayCalendarEntry entry, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
