namespace PhPayrollTimeApi.Application.Dtos;

public record WorkSchedulePatternDto(
    Guid Id,
    Guid EmployeeId,
    List<int> RestDays,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
