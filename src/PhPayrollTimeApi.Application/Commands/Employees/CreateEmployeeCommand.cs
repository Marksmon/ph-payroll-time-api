using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Employees;

public record CreateEmployeeCommand(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    UserRole Role,
    string JwtSubjectClaim);

public class CreateEmployeeCommandHandler : ICommandHandler<CreateEmployeeCommand>
{
    private readonly IEmployeeRepository _repo;

    public CreateEmployeeCommandHandler(IEmployeeRepository repo) => _repo = repo;

    public async Task HandleAsync(CreateEmployeeCommand cmd, CancellationToken ct)
    {
        if (await _repo.EmployeeNumberExistsAsync(cmd.EmployeeNumber, null, ct))
            throw new DomainValidationException($"Employee number '{cmd.EmployeeNumber}' is already in use.");

        if (await _repo.JwtSubjectClaimExistsAsync(cmd.JwtSubjectClaim, null, ct))
            throw new DomainValidationException($"JWT subject claim '{cmd.JwtSubjectClaim}' is already in use.");

        var now = DateTimeOffset.UtcNow;
        await _repo.AddAsync(new Employee
        {
            Id = cmd.Id,
            EmployeeNumber = cmd.EmployeeNumber,
            FullName = cmd.FullName,
            Role = cmd.Role,
            JwtSubjectClaim = cmd.JwtSubjectClaim,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        }, ct);

        await _repo.SaveAsync(ct);
    }
}
