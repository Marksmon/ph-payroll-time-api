using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfHolidayRepository : IHolidayRepository
{
    private readonly AppDbContext _db;

    public EfHolidayRepository(AppDbContext db) => _db = db;

    public Task<HolidayCalendarEntry?> GetByDateAsync(DateOnly date, CancellationToken ct) =>
        _db.HolidayCalendarEntries.FirstOrDefaultAsync(h => h.Date == date, ct);

    public Task<HolidayCalendarEntry?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.HolidayCalendarEntries.FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<List<HolidayCalendarEntry>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        _db.HolidayCalendarEntries
            .AsNoTracking()
            .Where(h => h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .ToListAsync(ct);

    public Task<bool> DateExistsAsync(DateOnly date, CancellationToken ct) =>
        _db.HolidayCalendarEntries.AnyAsync(h => h.Date == date, ct);

    public async Task AddAsync(HolidayCalendarEntry entry, CancellationToken ct) =>
        await _db.HolidayCalendarEntries.AddAsync(entry, ct);

    public Task DeleteAsync(HolidayCalendarEntry entry, CancellationToken ct)
    {
        _db.HolidayCalendarEntries.Remove(entry);
        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
