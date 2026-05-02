using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Employees;

public record UpdateEmployeeCommand(
    Guid Id,
    string FullName,
    string EmployeeNumber,
    string JwtSubjectClaim);

public class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand>
{
    private readonly IEmployeeRepository _repo;

    public UpdateEmployeeCommandHandler(IEmployeeRepository repo) => _repo = repo;

    public async Task HandleAsync(UpdateEmployeeCommand cmd, CancellationToken ct)
    {
        var employee = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new EntityNotFoundException("Employee", cmd.Id);

        if (await _repo.EmployeeNumberExistsAsync(cmd.EmployeeNumber, cmd.Id, ct))
            throw new DomainValidationException($"Employee number '{cmd.EmployeeNumber}' is already in use.");

        if (await _repo.JwtSubjectClaimExistsAsync(cmd.JwtSubjectClaim, cmd.Id, ct))
            throw new DomainValidationException($"JWT subject claim '{cmd.JwtSubjectClaim}' is already in use.");

        employee.FullName = cmd.FullName;
        employee.EmployeeNumber = cmd.EmployeeNumber;
        employee.JwtSubjectClaim = cmd.JwtSubjectClaim;
        employee.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.SaveAsync(ct);
    }
}
