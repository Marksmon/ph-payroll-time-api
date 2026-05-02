using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Domain.Services;

// Full implementation in Epic 5, Story 5.1
public class LogPairingService
{
    private readonly IClockProvider _clock;
    private readonly ILogClaimTracker _claimTracker;

    public LogPairingService(IClockProvider clock, ILogClaimTracker claimTracker)
    {
        _clock = clock;
        _claimTracker = claimTracker;
    }

    public (TimeLog? In, TimeLog? Out) PairLogs(
        ShiftSchedule schedule,
        IReadOnlyList<TimeLog> candidates)
        => throw new NotImplementedException("LogPairingService is implemented in Epic 5, Story 5.1.");
}
