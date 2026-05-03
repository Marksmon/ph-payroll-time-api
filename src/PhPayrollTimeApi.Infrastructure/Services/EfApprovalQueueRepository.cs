using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfApprovalQueueRepository : IApprovalQueueRepository
{
    private readonly AppDbContext _db;
    public EfApprovalQueueRepository(AppDbContext db) => _db = db;

    // ── Holiday Schedule Approvals ────────────────────────────────────────────

    public async Task<IReadOnlyList<HolidayScheduleApproval>> GetHolidayApprovalsByStatusAsync(
        ApprovalStatus status, CancellationToken ct)
        => await _db.HolidayScheduleApprovals
            .AsNoTracking()
            .Where(a => a.Status == status)
            .ToListAsync(ct);

    public async Task<HolidayScheduleApproval?> GetHolidayApprovalByIdAsync(Guid id, CancellationToken ct)
        => await _db.HolidayScheduleApprovals.FindAsync(new object[] { id }, ct);

    public async Task AddHolidayApprovalAsync(HolidayScheduleApproval approval, CancellationToken ct)
        => await _db.HolidayScheduleApprovals.AddAsync(approval, ct);

    // ── Rest Day Schedule Approvals ───────────────────────────────────────────

    public async Task<IReadOnlyList<RestDayScheduleApproval>> GetRestDayApprovalsByStatusAsync(
        ApprovalStatus status, CancellationToken ct)
        => await _db.RestDayScheduleApprovals
            .AsNoTracking()
            .Where(a => a.Status == status)
            .ToListAsync(ct);

    public async Task<RestDayScheduleApproval?> GetRestDayApprovalByIdAsync(Guid id, CancellationToken ct)
        => await _db.RestDayScheduleApprovals.FindAsync(new object[] { id }, ct);

    public async Task AddRestDayApprovalAsync(RestDayScheduleApproval approval, CancellationToken ct)
        => await _db.RestDayScheduleApprovals.AddAsync(approval, ct);

    // ── OT Approvals ──────────────────────────────────────────────────────────

    public async Task<OtApproval?> GetOtApprovalByIdAsync(Guid id, CancellationToken ct)
        => await _db.OtApprovals
            .Include(a => a.StagedActions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<OtApproval?> GetOtApprovalByScheduleIdAsync(Guid scheduleId, CancellationToken ct)
        => await _db.OtApprovals
            .Include(a => a.StagedActions)
            .FirstOrDefaultAsync(a => a.ShiftScheduleId == scheduleId, ct);

    public async Task<IReadOnlyList<OtApproval>> GetPendingOtApprovalsAsync(CancellationToken ct)
        => await _db.OtApprovals
            .AsNoTracking()
            .Include(a => a.StagedActions)
            .Where(a => a.Status == ApprovalStatus.PENDING)
            .ToListAsync(ct);

    public async Task AddOtApprovalAsync(OtApproval approval, CancellationToken ct)
        => await _db.OtApprovals.AddAsync(approval, ct);

    public async Task<IReadOnlyList<OtApproval>> GetOtApprovalsByDateAsync(DateOnly date, CancellationToken ct)
    {
        // Find OT approvals whose shift schedule start date matches
        var dateStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        var dateEnd = dateStart.AddDays(1);

        var scheduleIds = await _db.ShiftSchedules
            .AsNoTracking()
            .Where(s => s.ScheduleStart >= dateStart && s.ScheduleStart < dateEnd)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (!scheduleIds.Any()) return Array.Empty<OtApproval>();

        return await _db.OtApprovals
            .Where(a => scheduleIds.Contains(a.ShiftScheduleId)
                && a.Status == ApprovalStatus.APPROVED)
            .ToListAsync(ct);
    }

    // ── Staged OT Actions ─────────────────────────────────────────────────────

    public async Task<StagedOtAction?> GetStagedActionByIdAsync(Guid id, CancellationToken ct)
        => await _db.StagedOtActions.FindAsync(new object[] { id }, ct);

    public async Task RemoveStagedActionAsync(StagedOtAction action, CancellationToken ct)
    {
        _db.StagedOtActions.Remove(action);
        await Task.CompletedTask;
    }

    // ── Audit ─────────────────────────────────────────────────────────────────

    public async Task AddAuditRecordAsync(AuditRecord record, CancellationToken ct)
        => await _db.AuditRecords.AddAsync(record, ct);

    public async Task SaveAsync(CancellationToken ct)
        => await _db.SaveChangesAsync(ct);
}
