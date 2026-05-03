using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;

namespace PhPayrollTimeApi.Domain.ValueObjects;

public sealed class ComputationResult
{
    // Terminal classifications that bypass minute-conservation
    private static readonly HashSet<TimeSegmentClassification> TerminalClassifications = new()
    {
        TimeSegmentClassification.ABSENT,
        TimeSegmentClassification.IS_IN_CURRENT_SCHEDULE,
        TimeSegmentClassification.REGULAR_HOLIDAY_REST_PAID,
        TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_UNPAID,
    };

    private static readonly HashSet<TimeSegmentClassification> PaidHoursClassifications = new()
    {
        TimeSegmentClassification.NORMAL_PAID_HOURS,
        TimeSegmentClassification.NIGHT_DIFF_PAID_HOURS,
        TimeSegmentClassification.REGULAR_HOLIDAY_PAID_HOURS,
        TimeSegmentClassification.NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS,
        TimeSegmentClassification.SPECIAL_HOLIDAY_PAID_HOURS,
        TimeSegmentClassification.NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS,
        TimeSegmentClassification.REST_DAY_PAID_HOURS,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_PAID_HOURS,
        TimeSegmentClassification.REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM,
        TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM,
    };

    private static readonly HashSet<TimeSegmentClassification> EarlyOtClassifications = new()
    {
        TimeSegmentClassification.EARLY_OT,
        TimeSegmentClassification.NIGHT_DIFF_EARLY_OT,
        TimeSegmentClassification.REGULAR_HOLIDAY_EARLY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REGULAR_HOLIDAY_EARLY_OT,
        TimeSegmentClassification.SPECIAL_HOLIDAY_EARLY_OT,
        TimeSegmentClassification.NIGHT_DIFF_SPECIAL_HOLIDAY_EARLY_OT,
        TimeSegmentClassification.REST_DAY_EARLY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_EARLY_OT,
        TimeSegmentClassification.REST_DAY_REGULAR_HOLIDAY_EARLY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_EARLY_OT,
        TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_EARLY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_EARLY_OT,
    };

    private static readonly HashSet<TimeSegmentClassification> NormalOtClassifications = new()
    {
        TimeSegmentClassification.NORMAL_OT,
        TimeSegmentClassification.NIGHT_DIFF_OT,
        TimeSegmentClassification.REGULAR_HOLIDAY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REGULAR_HOLIDAY_OT,
        TimeSegmentClassification.SPECIAL_HOLIDAY_OT,
        TimeSegmentClassification.NIGHT_DIFF_SPECIAL_HOLIDAY_OT,
        TimeSegmentClassification.REST_DAY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_OT,
        TimeSegmentClassification.REST_DAY_REGULAR_HOLIDAY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_REGULAR_HOLIDAY_OT,
        TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_OT,
        TimeSegmentClassification.NIGHT_DIFF_REST_DAY_SPECIAL_HOLIDAY_OT,
    };

    public Guid ScheduleId { get; }
    public Guid EmployeeId { get; }
    public DateTimeOffset ScheduleStart { get; }
    public DateTimeOffset ScheduleEnd { get; }
    public DateTimeOffset? LogIn { get; }
    public DateTimeOffset? LogOut { get; }
    public int BreakDeductionMinutes { get; }
    public IReadOnlyList<TimeSegment> Segments { get; }
    public bool HrReviewFlagged { get; }

    public ComputationResult(
        Guid scheduleId,
        Guid employeeId,
        DateTimeOffset scheduleStart,
        DateTimeOffset scheduleEnd,
        DateTimeOffset? logIn,
        DateTimeOffset? logOut,
        int breakDeductionMinutes,
        IReadOnlyList<TimeSegment> segments,
        bool hrReviewFlagged)
    {
        var violations = new List<string>();
        bool hasTerminal = segments.Any(s => TerminalClassifications.Contains(s.Classification));

        // Invariant 3: No Zero-Duration
        foreach (var seg in segments)
        {
            if (seg.DurationMinutes <= 0)
                violations.Add($"Invariant 3 (No Zero-Duration): segment {seg.Classification} has DurationMinutes={seg.DurationMinutes}");
        }

        // Invariant 2: No Overlap
        var sorted = segments.OrderBy(s => s.Start).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].Start < sorted[i - 1].End)
                violations.Add($"Invariant 2 (No Overlap): {sorted[i - 1].Classification} ends {sorted[i - 1].End:O} but {sorted[i].Classification} starts {sorted[i].Start:O}");
        }

        // Invariant 1: Minute Conservation (non-terminal, both IN and OUT present, segments not all suppressed)
        if (!hasTerminal && segments.Count > 0 && logIn.HasValue && logOut.HasValue)
        {
            int expectedMinutes = (int)(logOut.Value - logIn.Value).TotalMinutes - breakDeductionMinutes;
            int actualMinutes = segments.Sum(s => s.DurationMinutes);
            if (actualMinutes != expectedMinutes)
                violations.Add($"Invariant 1 (Minute Conservation): expected {expectedMinutes} minutes (clocked={((int)(logOut.Value - logIn.Value).TotalMinutes)} - break={breakDeductionMinutes}) but segments sum to {actualMinutes}");
        }

        // Invariant 7: OT Bounds
        foreach (var seg in segments)
        {
            if (EarlyOtClassifications.Contains(seg.Classification) && seg.End > scheduleStart)
                violations.Add($"Invariant 7 (OT Bounds): EARLY_OT segment ends {seg.End:O} which is after schedule start {scheduleStart:O}");
            if (NormalOtClassifications.Contains(seg.Classification) && seg.Start < scheduleEnd)
                violations.Add($"Invariant 7 (OT Bounds): NORMAL_OT segment starts {seg.Start:O} which is before schedule end {scheduleEnd:O}");
        }

        // Invariant 8: Schedule-Hours Bounds (paid hours must be within schedule window)
        foreach (var seg in segments)
        {
            if (PaidHoursClassifications.Contains(seg.Classification))
            {
                if (seg.Start < scheduleStart)
                    violations.Add($"Invariant 8 (Schedule-Hours Bounds): PAID_HOURS segment starts {seg.Start:O} before schedule start {scheduleStart:O}");
                if (seg.End > scheduleEnd)
                    violations.Add($"Invariant 8 (Schedule-Hours Bounds): PAID_HOURS segment ends {seg.End:O} after schedule end {scheduleEnd:O}");
            }
        }

        if (violations.Count > 0)
            throw new ComputationInvariantException(violations);

        ScheduleId = scheduleId;
        EmployeeId = employeeId;
        ScheduleStart = scheduleStart;
        ScheduleEnd = scheduleEnd;
        LogIn = logIn;
        LogOut = logOut;
        BreakDeductionMinutes = breakDeductionMinutes;
        Segments = segments;
        HrReviewFlagged = hrReviewFlagged;
    }
}
