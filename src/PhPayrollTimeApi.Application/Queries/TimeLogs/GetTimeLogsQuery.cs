using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.TimeLogs;

public record GetTimeLogsQuery(Guid EmployeeId, DateTimeOffset StartDate, DateTimeOffset EndDate);

public class GetTimeLogsQueryHandler : IQueryHandler<GetTimeLogsQuery, List<TimeLogDto>>
{
    private readonly ITimeLogRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetTimeLogsQueryHandler(ITimeLogRepository repo, IEmployeeRepository employeeRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
    }

    public async Task<List<TimeLogDto>> HandleAsync(GetTimeLogsQuery query, CancellationToken ct)
    {
        if (await _employeeRepo.GetByIdAsync(query.EmployeeId, ct) is null)
            throw new EntityNotFoundException("Employee", query.EmployeeId);

        var logs = await _repo.GetByEmployeeAndDateRangeAsync(query.EmployeeId, query.StartDate, query.EndDate, ct);

        return logs.Select(l => new TimeLogDto(
            l.Id, l.EmployeeId, l.LogType.ToString(),
            l.LoggedAt, l.Source, l.CreatedAt)).ToList();
    }
}
