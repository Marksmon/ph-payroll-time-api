using PhPayrollTimeApi.Domain.Exceptions;

namespace PhPayrollTimeApi.Domain.ValueObjects;

public sealed class ComputationResult
{
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

        // Invariant 3: No Zero-Duration
        foreach (var seg in segments)
        {
            if (seg.DurationMinutes <= 0)
                violations.Add($"Invariant 3 (No Zero-Duration): segment {seg.Classification} has DurationMinutes={seg.DurationMinutes}");
        }

        // Invariant 2: No Overlap (check sorted segments don't overlap)
        var sorted = segments.OrderBy(s => s.Start).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].Start < sorted[i - 1].End)
                violations.Add($"Invariant 2 (No Overlap): {sorted[i - 1].Classification} ends {sorted[i - 1].End:O} but {sorted[i].Classification} starts {sorted[i].Start:O}");
        }

        // Invariants 1, 4-8 enforced fully in Epic 5 (ComputationEngine implementation)

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
