using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Employees;

public record DeactivateEmployeeCommand(Guid Id);

public class DeactivateEmployeeCommandHandler : ICommandHandler<DeactivateEmployeeCommand>
{
    private readonly IEmployeeRepository _repo;

    public DeactivateEmployeeCommandHandler(IEmployeeRepository repo) => _repo = repo;

    public async Task HandleAsync(DeactivateEmployeeCommand cmd, CancellationToken ct)
    {
        var employee = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new EntityNotFoundException("Employee", cmd.Id);

        employee.IsActive = false;
        employee.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.SaveAsync(ct);
    }
}
