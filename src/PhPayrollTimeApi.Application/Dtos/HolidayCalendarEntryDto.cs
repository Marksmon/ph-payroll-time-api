namespace PhPayrollTimeApi.Application.Dtos;

public record HolidayCalendarEntryDto(
    Guid Id,
    DateOnly Date,
    string Name,
    string Type,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
