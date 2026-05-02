namespace PhPayrollTimeApi.Application.Dtos;

public record ShiftScheduleDto(
    Guid Id,
    Guid EmployeeId,
    DateTimeOffset ScheduleStart,
    DateTimeOffset ScheduleEnd,
    List<BreakWindowDto> BreakWindows,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
