using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.ShiftSchedules;

public record GetShiftSchedulesQuery(Guid EmployeeId);

public class GetShiftSchedulesQueryHandler : IQueryHandler<GetShiftSchedulesQuery, List<ShiftScheduleDto>>
{
    private readonly IShiftScheduleRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetShiftSchedulesQueryHandler(IShiftScheduleRepository repo, IEmployeeRepository employeeRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
    }

    public async Task<List<ShiftScheduleDto>> HandleAsync(GetShiftSchedulesQuery query, CancellationToken ct)
    {
        if (await _employeeRepo.GetByIdAsync(query.EmployeeId, ct) is null)
            throw new EntityNotFoundException("Employee", query.EmployeeId);

        var schedules = await _repo.GetByEmployeeIdAsync(query.EmployeeId, ct);

        return schedules.Select(s => new ShiftScheduleDto(
            s.Id, s.EmployeeId, s.ScheduleStart, s.ScheduleEnd,
            s.BreakWindows.Select(b => new BreakWindowDto(b.BreakStart, b.BreakEnd)).ToList(),
            s.IsActive, s.CreatedAt, s.UpdatedAt)).ToList();
    }
}
