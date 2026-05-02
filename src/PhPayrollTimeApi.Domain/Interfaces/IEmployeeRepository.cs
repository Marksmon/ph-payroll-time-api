using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> EmployeeNumberExistsAsync(string employeeNumber, Guid? excludeId, CancellationToken ct);
    Task<bool> JwtSubjectClaimExistsAsync(string jwtSubjectClaim, Guid? excludeId, CancellationToken ct);
    Task AddAsync(Employee employee, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
