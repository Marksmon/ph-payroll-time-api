namespace PhPayrollTimeApi.Application.Dtos;

public record HolidayScheduleApprovalDto(
    Guid Id,
    Guid EmployeeId,
    Guid ShiftScheduleId,
    string HolidayDate,
    string Status,
    string? ApprovedBySubClaim,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record RestDayScheduleApprovalDto(
    Guid Id,
    Guid EmployeeId,
    Guid ShiftScheduleId,
    string RestDayDate,
    string Status,
    string? ApprovedBySubClaim,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record StagedOtActionDto(
    Guid Id,
    Guid OtApprovalId,
    string ActionType,
    string? SegmentClassification,
    int? AdjustedDurationMinutes,
    string? Reason,
    DateTimeOffset CreatedAt);

public record OtApprovalDto(
    Guid Id,
    Guid EmployeeId,
    Guid ShiftScheduleId,
    string Status,
    bool IsStale,
    string? CommittedBySubClaim,
    DateTimeOffset? CommittedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<StagedOtActionDto> StagedActions);
