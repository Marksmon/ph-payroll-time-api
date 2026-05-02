using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.Entities;

public class OtApproval
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ShiftScheduleId { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.PENDING;
    public string? CommittedBySubClaim { get; set; }
    public DateTimeOffset? CommittedAt { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<StagedOtAction> StagedActions { get; set; } = new List<StagedOtAction>();
}
