namespace PhPayrollTimeApi.Application.Dtos;

public record TimeLogDto(
    Guid Id,
    Guid EmployeeId,
    string LogType,
    DateTimeOffset LoggedAt,
    string? Source,
    DateTimeOffset CreatedAt);
