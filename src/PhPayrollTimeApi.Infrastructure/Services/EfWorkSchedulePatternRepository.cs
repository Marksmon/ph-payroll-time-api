using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfWorkSchedulePatternRepository : IWorkSchedulePatternRepository
{
    private readonly AppDbContext _db;

    public EfWorkSchedulePatternRepository(AppDbContext db) => _db = db;

    public Task<WorkSchedulePattern?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.WorkSchedulePatterns.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<WorkSchedulePattern>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct) =>
        _db.WorkSchedulePatterns
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderBy(p => p.EffectiveDate)
            .ToListAsync(ct);

    public async Task AddAsync(WorkSchedulePattern pattern, CancellationToken ct) =>
        await _db.WorkSchedulePatterns.AddAsync(pattern, ct);

    public async Task AddRangeAsync(IEnumerable<WorkSchedulePattern> patterns, CancellationToken ct) =>
        await _db.WorkSchedulePatterns.AddRangeAsync(patterns, ct);

    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
