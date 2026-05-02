using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.Entities;

public class HolidayScheduleApproval
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ShiftScheduleId { get; set; }
    public DateOnly HolidayDate { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.PENDING;
    public string? ApprovedBySubClaim { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
