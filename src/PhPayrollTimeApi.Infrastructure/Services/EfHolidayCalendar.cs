using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfHolidayCalendar : IHolidayCalendar
{
    private readonly AppDbContext _db;

    public EfHolidayCalendar(AppDbContext db) => _db = db;

    public async Task<HolidayType> GetHolidayTypeAsync(DateOnly date, CancellationToken ct)
    {
        var entry = await _db.HolidayCalendarEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Date == date, ct);

        return entry?.Type ?? HolidayType.NONE;
    }
}
