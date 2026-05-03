namespace PhPayrollTimeApi.Application.Dtos;

public record ComputationResultDto(
    Guid ScheduleId,
    Guid EmployeeId,
    DateTimeOffset ScheduleStart,
    DateTimeOffset ScheduleEnd,
    DateTimeOffset? LogIn,
    DateTimeOffset? LogOut,
    int BreakDeductionMinutes,
    bool HrReviewFlagged,
    IReadOnlyList<TimeSegmentDto> Segments);

public record TimeSegmentDto(
    DateTimeOffset Start,
    DateTimeOffset End,
    int DurationMinutes,
    string Classification,
    string ApprovalStatus);
