using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Domain.Services;

public class LogPairingService
{
    private static readonly TimeSpan ManilaOffset = TimeSpan.FromHours(8);
    private const int DefaultOtLookbackDays = 1;

    private readonly IClockProvider _clock;
    private readonly ILogClaimTracker _claimTracker;

    public LogPairingService(IClockProvider clock, ILogClaimTracker claimTracker)
    {
        _clock = clock;
        _claimTracker = claimTracker;
    }

    // Returns (In?, Out?). Null In = no valid IN found (ABSENT).
    // Null Out with non-null In = caller must check server time vs schedule end for ABSENT/IS_IN_CURRENT_SCHEDULE.
    public (TimeLog? In, TimeLog? Out) PairLogs(
        ShiftSchedule schedule,
        IReadOnlyList<TimeLog> candidates,
        int otLookbackDays = DefaultOtLookbackDays)
    {
        var scheduleStartManila = ToManila(schedule.ScheduleStart);
        var scheduleDate = DateOnly.FromDateTime(scheduleStartManila.DateTime);

        // Search for IN log: same Manila day or up to otLookbackDays prior
        TimeLog? inLog = null;
        for (int lookback = 0; lookback <= otLookbackDays; lookback++)
        {
            var targetDate = scheduleDate.AddDays(-lookback);
            var inCandidate = candidates
                .Where(l => l.LogType == LogType.IN
                    && !_claimTracker.IsClaimed(l.Id)
                    && DateOnly.FromDateTime(ToManila(l.LoggedAt).DateTime) == targetDate
                    && l.LoggedAt < schedule.ScheduleEnd)
                .OrderBy(l => l.LoggedAt)
                .FirstOrDefault();

            if (inCandidate is not null)
            {
                inLog = inCandidate;
                break;
            }
        }

        if (inLog is null) return (null, null);

        _claimTracker.Claim(inLog.Id);

        // Search for OUT log: first unclaimed OUT after IN
        var outLog = candidates
            .Where(l => l.LogType == LogType.OUT
                && !_claimTracker.IsClaimed(l.Id)
                && l.LoggedAt > inLog.LoggedAt)
            .OrderBy(l => l.LoggedAt)
            .FirstOrDefault();

        if (outLog is not null)
            _claimTracker.Claim(outLog.Id);

        return (inLog, outLog);
    }

    // Returns the terminal classification for a schedule with no paired IN log found.
    // Called by ComputationEngine when PairLogs returns (null, null).
    public bool IsInProgress(ShiftSchedule schedule)
        => _clock.UtcNow < schedule.ScheduleEnd;

    private static DateTimeOffset ToManila(DateTimeOffset utc) => utc.ToOffset(ManilaOffset);
}
