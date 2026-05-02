using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Domain.ValueObjects;

namespace PhPayrollTimeApi.Domain.Services;

// Full implementation in Epic 5 (Stories 5.1–5.5)
public class ComputationEngine
{
    private readonly IHolidayCalendar _holidayCalendar;
    private readonly IClockProvider _clock;
    private readonly IFeatureFlagProvider _featureFlags;
    private readonly ILogClaimTracker _logClaimTracker;
    private readonly IHolidayApprovalRepository _approvalRepository;

    public ComputationEngine(
        IHolidayCalendar holidayCalendar,
        IClockProvider clock,
        IFeatureFlagProvider featureFlags,
        ILogClaimTracker logClaimTracker,
        IHolidayApprovalRepository approvalRepository)
    {
        _holidayCalendar = holidayCalendar;
        _clock = clock;
        _featureFlags = featureFlags;
        _logClaimTracker = logClaimTracker;
        _approvalRepository = approvalRepository;
    }

    public Task<ComputationResult> ComputeAsync(
        ShiftSchedule schedule,
        WorkSchedulePattern workPattern,
        IReadOnlyList<TimeLog> logs,
        CancellationToken ct)
        => throw new NotImplementedException("ComputationEngine is implemented in Epic 5.");
}
