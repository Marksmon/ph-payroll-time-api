using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IHolidayCalendar
{
    Task<HolidayType> GetHolidayTypeAsync(DateOnly date, CancellationToken ct);
}
