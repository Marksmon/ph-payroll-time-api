using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using Xunit;
using static PhPayrollTimeApi.Domain.Tests.ComputationEngine.ComputationTestHelper;

namespace PhPayrollTimeApi.Domain.Tests.ComputationEngine;

public class RegularDayTests
{
    // Standard shift: 8am–5pm Manila (UTC 00:00–09:00 on 2026-05-06)
    private static readonly DateTimeOffset ShiftStart = ManilaMidnight.AddHours(8).ToUniversalTime();
    private static readonly DateTimeOffset ShiftEnd   = ManilaMidnight.AddHours(17).ToUniversalTime();

    [Fact]
    public void Compute_WhenFullShiftWorked_EmitsNormalPaidHours()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
        Assert.DoesNotContain(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_OT);
    }

    [Fact]
    public void Compute_WhenClockedInEarly_EmitsEarlyOt()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var earlyIn = ShiftStart.AddHours(-1);
        var logs = new[] { InLog(earlyIn), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.EARLY_OT);
        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenClockedOutLate_EmitsNormalOt()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var lateOut = ShiftEnd.AddHours(2);
        var logs = new[] { InLog(ShiftStart), OutLog(lateOut) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_OT);
        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenNoLogs_EmitsAbsent()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();

        var result = engine.ComputeAsync(schedule, null, Array.Empty<TimeLog>(), CancellationToken.None).GetAwaiter().GetResult();

        Assert.Single(result.Segments);
        Assert.Equal(TimeSegmentClassification.ABSENT, result.Segments[0].Classification);
    }

    [Fact]
    public void Compute_WhenInFoundButNoOut_BeforeScheduleEnd_EmitsIsInCurrentSchedule()
    {
        var beforeEnd = ShiftEnd.AddHours(-2);
        var engine = BuildEngine(now: beforeEnd);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Single(result.Segments);
        Assert.Equal(TimeSegmentClassification.IS_IN_CURRENT_SCHEDULE, result.Segments[0].Classification);
    }

    [Fact]
    public void Compute_WhenInFoundButNoOut_AfterScheduleEnd_EmitsAbsent()
    {
        var afterEnd = ShiftEnd.AddHours(1);
        var engine = BuildEngine(now: afterEnd);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Single(result.Segments);
        Assert.Equal(TimeSegmentClassification.ABSENT, result.Segments[0].Classification);
    }

    [Fact]
    public void Compute_WhenBreakOverlapsClocking_DeductsBreak()
    {
        var engine = BuildEngine();
        // Break: 12pm–1pm Manila
        var breakStart = ManilaMidnight.AddHours(12).ToUniversalTime();
        var breakEnd   = ManilaMidnight.AddHours(13).ToUniversalTime();
        var schedule = ShiftWithBreak(breakStart, breakEnd);
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(60, result.BreakDeductionMinutes);
    }

    [Fact]
    public void Compute_WhenLogsPresent_HrReviewFlagged()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.True(result.HrReviewFlagged);
    }

    [Fact]
    public void Compute_WhenNoLogs_HrReviewNotFlagged()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();

        var result = engine.ComputeAsync(schedule, null, Array.Empty<TimeLog>(), CancellationToken.None).GetAwaiter().GetResult();

        Assert.False(result.HrReviewFlagged);
    }
}
