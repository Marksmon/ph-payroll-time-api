using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.ValueObjects;

public sealed record TimeSegment(
    DateTimeOffset Start,
    DateTimeOffset End,
    int DurationMinutes,
    TimeSegmentClassification Classification,
    ApprovalStatus ApprovalStatus);
