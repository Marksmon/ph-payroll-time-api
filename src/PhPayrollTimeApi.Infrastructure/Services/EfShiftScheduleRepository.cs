using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfShiftScheduleRepository : IShiftScheduleRepository
{
    private readonly AppDbContext _db;

    public EfShiftScheduleRepository(AppDbContext db) => _db = db;

    public Task<ShiftSchedule?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.ShiftSchedules
            .Include(s => s.BreakWindows)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<ShiftSchedule>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct) =>
        _db.ShiftSchedules
            .AsNoTracking()
            .Include(s => s.BreakWindows)
            .Where(s => s.EmployeeId == employeeId)
            .OrderBy(s => s.ScheduleStart)
            .ToListAsync(ct);

    public Task<bool> HasOverlapAsync(Guid employeeId, DateTimeOffset start, DateTimeOffset end, Guid? excludeId, CancellationToken ct) =>
        _db.ShiftSchedules.AnyAsync(s =>
            s.EmployeeId == employeeId &&
            (excludeId == null || s.Id != excludeId) &&
            s.ScheduleStart < end &&
            s.ScheduleEnd > start, ct);

    public async Task AddAsync(ShiftSchedule schedule, CancellationToken ct) =>
        await _db.ShiftSchedules.AddAsync(schedule, ct);

    public Task DeleteAsync(ShiftSchedule schedule, CancellationToken ct)
    {
        _db.ShiftSchedules.Remove(schedule);
        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
