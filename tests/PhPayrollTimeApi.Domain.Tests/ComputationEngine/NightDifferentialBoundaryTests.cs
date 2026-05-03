using PhPayrollTimeApi.Domain.Enums;
using Xunit;
using static PhPayrollTimeApi.Domain.Tests.ComputationEngine.ComputationTestHelper;

namespace PhPayrollTimeApi.Domain.Tests.ComputationEngine;

public class NightDifferentialBoundaryTests
{
    // Night shift: 10pm–6am Manila (crosses midnight)
    // ManilaMidnight is 2026-05-06 00:00 Manila = 2026-05-05 16:00 UTC
    private static readonly DateTimeOffset NightShiftStart = ManilaMidnight.AddHours(22 - 24).ToUniversalTime(); // 10pm Manila day-before
    private static readonly DateTimeOffset NightShiftEnd   = ManilaMidnight.AddHours(6).ToUniversalTime();       // 6am Manila next day

    [Fact]
    public void Compute_WhenSegmentCrosses10pmBoundary_SplitsAtNightDiffStart()
    {
        // Evening shift: 8pm–11pm Manila (crosses 10pm)
        var start = ManilaMidnight.AddHours(20 - 24).ToUniversalTime(); // 8pm Manila prev day
        var end   = ManilaMidnight.AddHours(23 - 24).ToUniversalTime(); // 11pm Manila prev day
        var engine = BuildEngine(now: end.AddHours(2));

        var schedule = new Domain.Entities.ShiftSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            ScheduleStart = start,
            ScheduleEnd   = end,
            BreakWindows  = new List<Domain.Entities.BreakWindow>(),
            IsActive = true
        };
        var logs = new[] { InLog(start), OutLog(end) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NIGHT_DIFF_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenSegmentEntirelyWithinNightDiff_EmitsNightDiffVariant()
    {
        // 11pm–2am Manila — entirely within night diff window
        var start = ManilaMidnight.AddHours(23 - 24).ToUniversalTime(); // 11pm Manila prev day
        var end   = ManilaMidnight.AddHours(2).ToUniversalTime();        // 2am Manila
        var engine = BuildEngine(now: end.AddHours(2));

        var schedule = new Domain.Entities.ShiftSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            ScheduleStart = start,
            ScheduleEnd   = end,
            BreakWindows  = new List<Domain.Entities.BreakWindow>(),
            IsActive = true
        };
        var logs = new[] { InLog(start), OutLog(end) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.All(result.Segments, s =>
            Assert.True(s.Classification == TimeSegmentClassification.NIGHT_DIFF_PAID_HOURS,
                $"Expected NIGHT_DIFF_PAID_HOURS but got {s.Classification}"));
    }

    [Fact]
    public void Compute_WhenSegmentCrosses6amBoundary_SplitsAtNightDiffEnd()
    {
        // 4am–8am Manila — crosses 6am
        var start = ManilaMidnight.AddHours(4).ToUniversalTime();
        var end   = ManilaMidnight.AddHours(8).ToUniversalTime();
        var engine = BuildEngine(now: end.AddHours(2));

        var schedule = new Domain.Entities.ShiftSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            ScheduleStart = start,
            ScheduleEnd   = end,
            BreakWindows  = new List<Domain.Entities.BreakWindow>(),
            IsActive = true
        };
        var logs = new[] { InLog(start), OutLog(end) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NIGHT_DIFF_PAID_HOURS);
        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenSegmentEntirelyOutsideNightDiff_EmitsNoNightDiffVariant()
    {
        // Standard 9am–5pm Manila — entirely outside night diff
        var engine = BuildEngine();
        var schedule = StandardShift();
        var logs = new[] { InLog(schedule.ScheduleStart), OutLog(schedule.ScheduleEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.DoesNotContain(result.Segments, s =>
            s.Classification.ToString().StartsWith("NIGHT_DIFF"));
    }
}
