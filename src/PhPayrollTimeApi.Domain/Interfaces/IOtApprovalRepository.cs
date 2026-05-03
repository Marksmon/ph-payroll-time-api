using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IOtApprovalRepository
{
    Task<OtApproval?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<OtApproval?> GetByScheduleIdAsync(Guid scheduleId, CancellationToken ct);
    Task<IReadOnlyList<OtApproval>> GetPendingByManagerAsync(string managerSub, CancellationToken ct);
    Task<IReadOnlyList<OtApproval>> GetByDateAsync(DateOnly date, CancellationToken ct);
    Task AddAsync(OtApproval approval, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
