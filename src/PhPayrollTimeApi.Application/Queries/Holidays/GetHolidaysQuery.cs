using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.Holidays;

public record GetHolidaysQuery(DateOnly StartDate, DateOnly EndDate);

public class GetHolidaysQueryHandler : IQueryHandler<GetHolidaysQuery, List<HolidayCalendarEntryDto>>
{
    private readonly IHolidayRepository _repo;

    public GetHolidaysQueryHandler(IHolidayRepository repo) => _repo = repo;

    public async Task<List<HolidayCalendarEntryDto>> HandleAsync(GetHolidaysQuery query, CancellationToken ct)
    {
        var entries = await _repo.GetByDateRangeAsync(query.StartDate, query.EndDate, ct);

        return entries.Select(e => new HolidayCalendarEntryDto(
            e.Id, e.Date, e.Name, e.Type.ToString(),
            e.CreatedAt, e.UpdatedAt)).ToList();
    }
}
