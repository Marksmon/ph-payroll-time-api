using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.Employees;

public record GetEmployeeQuery(Guid Id);

public class GetEmployeeQueryHandler : IQueryHandler<GetEmployeeQuery, EmployeeDto>
{
    private readonly IEmployeeRepository _repo;

    public GetEmployeeQueryHandler(IEmployeeRepository repo) => _repo = repo;

    public async Task<EmployeeDto> HandleAsync(GetEmployeeQuery query, CancellationToken ct)
    {
        var emp = await _repo.GetByIdAsync(query.Id, ct)
            ?? throw new EntityNotFoundException("Employee", query.Id);

        return new EmployeeDto(
            emp.Id, emp.EmployeeNumber, emp.FullName,
            emp.Role.ToString(), emp.JwtSubjectClaim,
            emp.IsActive, emp.CreatedAt, emp.UpdatedAt);
    }
}
