using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.Entities;

public class StagedOtAction
{
    public Guid Id { get; set; }
    public Guid OtApprovalId { get; set; }
    public OtApproval? OtApproval { get; set; }
    public StagedOtActionType ActionType { get; set; }
    public TimeSegmentClassification? SegmentClassification { get; set; }
    public int? AdjustedDurationMinutes { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
