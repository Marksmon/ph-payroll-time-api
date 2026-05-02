using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfTimeLogRepository : ITimeLogRepository
{
    private readonly AppDbContext _db;

    public EfTimeLogRepository(AppDbContext db) => _db = db;

    public Task<List<TimeLog>> GetByEmployeeAndDateRangeAsync(
        Guid employeeId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        _db.TimeLogs
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId && l.LoggedAt >= from && l.LoggedAt <= to)
            .OrderBy(l => l.LoggedAt)
            .ToListAsync(ct);

    public async Task AddAsync(TimeLog log, CancellationToken ct) =>
        await _db.TimeLogs.AddAsync(log, ct);

    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
