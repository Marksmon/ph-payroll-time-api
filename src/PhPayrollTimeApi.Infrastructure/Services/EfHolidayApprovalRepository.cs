using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfHolidayApprovalRepository : IHolidayApprovalRepository
{
    private readonly AppDbContext _db;

    public EfHolidayApprovalRepository(AppDbContext db) => _db = db;

    public async Task<HolidayScheduleApproval?> GetHolidayScheduleApprovalAsync(
        Guid shiftScheduleId, CancellationToken ct)
        => await _db.HolidayScheduleApprovals
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.ShiftScheduleId == shiftScheduleId, ct);

    public async Task<RestDayScheduleApproval?> GetRestDayScheduleApprovalAsync(
        Guid shiftScheduleId, CancellationToken ct)
        => await _db.RestDayScheduleApprovals
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ShiftScheduleId == shiftScheduleId, ct);

    public async Task<IReadOnlyList<HolidayScheduleApproval>> GetHolidayScheduleApprovalsByEmployeeAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct)
        => await _db.HolidayScheduleApprovals
            .AsNoTracking()
            .Where(h => h.EmployeeId == employeeId && h.HolidayDate >= from && h.HolidayDate <= to)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RestDayScheduleApproval>> GetRestDayScheduleApprovalsByEmployeeAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct)
        => await _db.RestDayScheduleApprovals
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.RestDayDate >= from && r.RestDayDate <= to)
            .ToListAsync(ct);
}
