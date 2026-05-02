using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.WorkSchedulePatterns;

public record GetWorkSchedulePatternsQuery(Guid EmployeeId);

public class GetWorkSchedulePatternsQueryHandler : IQueryHandler<GetWorkSchedulePatternsQuery, List<WorkSchedulePatternDto>>
{
    private readonly IWorkSchedulePatternRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetWorkSchedulePatternsQueryHandler(
        IWorkSchedulePatternRepository repo,
        IEmployeeRepository employeeRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
    }

    public async Task<List<WorkSchedulePatternDto>> HandleAsync(GetWorkSchedulePatternsQuery query, CancellationToken ct)
    {
        if (await _employeeRepo.GetByIdAsync(query.EmployeeId, ct) is null)
            throw new EntityNotFoundException("Employee", query.EmployeeId);

        var patterns = await _repo.GetByEmployeeIdAsync(query.EmployeeId, ct);

        return patterns.Select(p => new WorkSchedulePatternDto(
            p.Id, p.EmployeeId,
            p.RestDays.Select(d => (int)d).ToList(),
            p.EffectiveDate, p.ExpiryDate,
            p.CreatedAt, p.UpdatedAt)).ToList();
    }
}
