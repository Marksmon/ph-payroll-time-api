namespace PhPayrollTimeApi.Application.Dtos;

public record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string Role,
    string JwtSubjectClaim,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
