using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.ValueObjects;
using Xunit;

namespace PhPayrollTimeApi.Domain.Tests.ComputationEngine;

public class InvariantEnforcementTests
{
    private static readonly DateTimeOffset T0 = new(2026, 5, 6, 0, 0, 0, TimeSpan.Zero);

    private static ComputationResult Build(
        DateTimeOffset? logIn, DateTimeOffset? logOut,
        int break_, IReadOnlyList<TimeSegment> segments,
        DateTimeOffset? schedStart = null, DateTimeOffset? schedEnd = null)
    {
        var ss = schedStart ?? T0;
        var se = schedEnd ?? T0.AddHours(9);
        return new ComputationResult(Guid.NewGuid(), Guid.NewGuid(), ss, se, logIn, logOut, break_, segments, false);
    }

    // Invariant 3 — No Zero-Duration

    [Fact]
    public void Invariant3_WhenPositiveDuration_Passes()
    {
        var seg = new TimeSegment(T0, T0.AddHours(4), 240, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED);
        var result = Build(T0, T0.AddHours(4), 0, new[] { seg });
        Assert.Single(result.Segments);
    }

    [Fact]
    public void Invariant3_WhenZeroDuration_ThrowsWithInvariant3()
    {
        var seg = new TimeSegment(T0, T0, 0, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED);
        var ex = Assert.Throws<ComputationInvariantException>(() => Build(null, null, 0, new[] { seg }));
        Assert.Contains("Invariant 3", ex.Message);
        Assert.True(ex.Violations.Count >= 1);
    }

    // Invariant 2 — No Overlap

    [Fact]
    public void Invariant2_WhenNoOverlap_Passes()
    {
        var segs = new[]
        {
            new TimeSegment(T0, T0.AddHours(4), 240, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
            new TimeSegment(T0.AddHours(4), T0.AddHours(8), 240, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };
        var result = Build(T0, T0.AddHours(8), 0, segs);
        Assert.Equal(2, result.Segments.Count);
    }

    [Fact]
    public void Invariant2_WhenOverlap_ThrowsWithInvariant2()
    {
        var segs = new[]
        {
            new TimeSegment(T0, T0.AddHours(5), 300, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
            new TimeSegment(T0.AddHours(4), T0.AddHours(9), 300, TimeSegmentClassification.NORMAL_OT, ApprovalStatus.PENDING),
        };
        var ex = Assert.Throws<ComputationInvariantException>(() => Build(null, null, 0, segs));
        Assert.Contains("Invariant 2", ex.Message);
    }

    // Invariant 1 — Minute Conservation

    [Fact]
    public void Invariant1_WhenSegmentsSumMatchesClockedMinusBreak_Passes()
    {
        // logIn=T0, logOut=T0+8h → 480 min; break=60; expected segments=420
        var segs = new[]
        {
            new TimeSegment(T0, T0.AddHours(7), 420, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };
        var result = Build(T0, T0.AddHours(8), 60, segs);
        Assert.Single(result.Segments);
    }

    [Fact]
    public void Invariant1_WhenSegmentsDontMatchClockedMinusBreak_ThrowsWithInvariant1()
    {
        // logIn=T0, logOut=T0+8h → 480 min; break=0; segments=400 (wrong)
        var segs = new[]
        {
            new TimeSegment(T0, T0.AddMinutes(400), 400, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };
        var ex = Assert.Throws<ComputationInvariantException>(() => Build(T0, T0.AddHours(8), 0, segs));
        Assert.Contains("Invariant 1", ex.Message);
    }

    // Invariant 7 — OT Bounds

    [Fact]
    public void Invariant7_WhenEarlyOtBeforeScheduleStart_Passes()
    {
        // Schedule starts at T0+8h; early OT from T0+7h to T0+8h = 60min; paid T0+8h to T0+9h = 60min
        var ss = T0.AddHours(8);
        var se = T0.AddHours(9);
        var segs = new[]
        {
            new TimeSegment(T0.AddHours(7), ss, 60, TimeSegmentClassification.EARLY_OT, ApprovalStatus.APPROVED),
            new TimeSegment(ss, se, 60, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };
        var result = Build(T0.AddHours(7), se, 0, segs, ss, se);
        Assert.Equal(2, result.Segments.Count);
    }

    [Fact]
    public void Invariant7_WhenEarlyOtExceedsScheduleStart_ThrowsWithInvariant7()
    {
        var ss = T0.AddHours(8);
        var se = T0.AddHours(9);
        // EARLY_OT ends AFTER schedule start → violation
        var segs = new[]
        {
            new TimeSegment(T0.AddHours(7), T0.AddHours(9), 120, TimeSegmentClassification.EARLY_OT, ApprovalStatus.APPROVED),
        };
        var ex = Assert.Throws<ComputationInvariantException>(() => Build(T0.AddHours(7), T0.AddHours(9), 0, segs, ss, se));
        Assert.Contains("Invariant 7", ex.Message);
    }

    // Invariant 8 — Schedule-Hours Bounds

    [Fact]
    public void Invariant8_WhenPaidHoursWithinSchedule_Passes()
    {
        var ss = T0.AddHours(8);
        var se = T0.AddHours(17);
        var segs = new[]
        {
            new TimeSegment(ss, se, 540, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };
        var result = Build(ss, se, 0, segs, ss, se);
        Assert.Single(result.Segments);
    }

    [Fact]
    public void Invariant8_WhenPaidHoursExceedsScheduleEnd_ThrowsWithInvariant8()
    {
        var ss = T0.AddHours(8);
        var se = T0.AddHours(17);
        var segs = new[]
        {
            new TimeSegment(ss, se.AddHours(1), 600, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };
        var ex = Assert.Throws<ComputationInvariantException>(() => Build(ss, se.AddHours(1), 0, segs, ss, se));
        Assert.Contains("Invariant 8", ex.Message);
    }

    // All violations collected before throwing

    [Fact]
    public void Invariant_WhenMultipleViolations_CollectsAllBeforeThrowing()
    {
        // Zero duration (Inv 3) + overlap (Inv 2)
        var segs = new[]
        {
            new TimeSegment(T0, T0, 0, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
            new TimeSegment(T0, T0.AddHours(4), 240, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
            new TimeSegment(T0.AddHours(3), T0.AddHours(8), 300, TimeSegmentClassification.NORMAL_OT, ApprovalStatus.PENDING),
        };
        var ex = Assert.Throws<ComputationInvariantException>(() => Build(null, null, 0, segs));
        Assert.True(ex.Violations.Count >= 2, $"Expected >=2 violations but got {ex.Violations.Count}: {ex.Message}");
    }
}
