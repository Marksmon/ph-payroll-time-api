using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Domain.Services;

namespace PhPayrollTimeApi.Application.Queries.GetComputation;

public record GetComputationQuery(Guid ScheduleId);

public class GetComputationQueryHandler : IQueryHandler<GetComputationQuery, ComputationResultDto>
{
    private readonly IShiftScheduleRepository _scheduleRepo;
    private readonly IWorkSchedulePatternRepository _patternRepo;
    private readonly ITimeLogRepository _logRepo;
    private readonly ComputationEngine _engine;

    public GetComputationQueryHandler(
        IShiftScheduleRepository scheduleRepo,
        IWorkSchedulePatternRepository patternRepo,
        ITimeLogRepository logRepo,
        ComputationEngine engine)
    {
        _scheduleRepo = scheduleRepo;
        _patternRepo = patternRepo;
        _logRepo = logRepo;
        _engine = engine;
    }

    public async Task<ComputationResultDto> HandleAsync(GetComputationQuery query, CancellationToken ct)
    {
        var schedule = await _scheduleRepo.GetByIdAsync(query.ScheduleId, ct)
            ?? throw new EntityNotFoundException("ShiftSchedule", query.ScheduleId);

        // Load active work schedule pattern for the employee on the shift date
        var patterns = await _patternRepo.GetByEmployeeIdAsync(schedule.EmployeeId, ct);
        var shiftDate = DateOnly.FromDateTime(schedule.ScheduleStart.UtcDateTime);
        var pattern = patterns
            .Where(p => p.EffectiveDate <= shiftDate
                && (p.ExpiryDate is null || p.ExpiryDate >= shiftDate))
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefault();

        // Load logs: from 1 day before schedule start to 1 day after schedule end (covers prior-day IN)
        var from = schedule.ScheduleStart.AddDays(-1);
        var to = schedule.ScheduleEnd.AddDays(1);
        var logs = await _logRepo.GetByEmployeeAndDateRangeAsync(schedule.EmployeeId, from, to, ct);

        var result = await _engine.ComputeAsync(schedule, pattern, logs, ct);

        return new ComputationResultDto(
            result.ScheduleId,
            result.EmployeeId,
            result.ScheduleStart,
            result.ScheduleEnd,
            result.LogIn,
            result.LogOut,
            result.BreakDeductionMinutes,
            result.HrReviewFlagged,
            result.Segments.Select(s => new TimeSegmentDto(
                s.Start, s.End, s.DurationMinutes,
                s.Classification.ToString(),
                s.ApprovalStatus.ToString())).ToList());
    }
}
