using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IApprovalQueueRepository
{
    // Holiday Schedule Approvals
    Task<IReadOnlyList<HolidayScheduleApproval>> GetHolidayApprovalsByStatusAsync(ApprovalStatus status, CancellationToken ct);
    Task<HolidayScheduleApproval?> GetHolidayApprovalByIdAsync(Guid id, CancellationToken ct);
    Task AddHolidayApprovalAsync(HolidayScheduleApproval approval, CancellationToken ct);

    // Rest Day Schedule Approvals
    Task<IReadOnlyList<RestDayScheduleApproval>> GetRestDayApprovalsByStatusAsync(ApprovalStatus status, CancellationToken ct);
    Task<RestDayScheduleApproval?> GetRestDayApprovalByIdAsync(Guid id, CancellationToken ct);
    Task AddRestDayApprovalAsync(RestDayScheduleApproval approval, CancellationToken ct);

    // OT Approvals
    Task<OtApproval?> GetOtApprovalByIdAsync(Guid id, CancellationToken ct);
    Task<OtApproval?> GetOtApprovalByScheduleIdAsync(Guid scheduleId, CancellationToken ct);
    Task<IReadOnlyList<OtApproval>> GetPendingOtApprovalsAsync(CancellationToken ct);
    Task AddOtApprovalAsync(OtApproval approval, CancellationToken ct);
    Task<IReadOnlyList<OtApproval>> GetOtApprovalsByDateAsync(DateOnly date, CancellationToken ct);

    // Staged OT Actions
    Task<StagedOtAction?> GetStagedActionByIdAsync(Guid id, CancellationToken ct);
    Task RemoveStagedActionAsync(StagedOtAction action, CancellationToken ct);

    // Audit
    Task AddAuditRecordAsync(AuditRecord record, CancellationToken ct);

    Task SaveAsync(CancellationToken ct);
}
