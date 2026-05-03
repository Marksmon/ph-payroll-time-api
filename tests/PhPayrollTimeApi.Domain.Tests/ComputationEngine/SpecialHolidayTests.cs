using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using Xunit;
using static PhPayrollTimeApi.Domain.Tests.ComputationEngine.ComputationTestHelper;

namespace PhPayrollTimeApi.Domain.Tests.ComputationEngine;

public class SpecialHolidayTests
{
    private static readonly DateTimeOffset ShiftStart = ManilaMidnight.AddHours(8).ToUniversalTime();
    private static readonly DateTimeOffset ShiftEnd   = ManilaMidnight.AddHours(17).ToUniversalTime();

    [Fact]
    public void Compute_WhenSpecialNonWorkingNoLogsNoApproval_ReturnsEmptySegments()
    {
        var engine = BuildEngine(HolidayType.SPECIAL_NON_WORKING);
        var schedule = StandardShift();

        var result = engine.ComputeAsync(schedule, null, Array.Empty<TimeLog>(), CancellationToken.None).GetAwaiter().GetResult();

        Assert.Empty(result.Segments);
    }

    [Fact]
    public void Compute_WhenSpecialNonWorkingWithLogs_EmitsSpecialHolidayPaidHours()
    {
        var engine = BuildEngine(HolidayType.SPECIAL_NON_WORKING, holidayPayEnabled: true);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.SPECIAL_HOLIDAY_PAID_HOURS);
        Assert.True(result.HrReviewFlagged);
    }

    [Fact]
    public void Compute_WhenSpecialNonWorkingFlagDisabled_NoPaidHoursSegment()
    {
        var engine = BuildEngine(HolidayType.SPECIAL_NON_WORKING, holidayPayEnabled: false);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.DoesNotContain(result.Segments, s =>
            s.Classification == TimeSegmentClassification.SPECIAL_HOLIDAY_PAID_HOURS ||
            s.Classification == TimeSegmentClassification.NIGHT_DIFF_SPECIAL_HOLIDAY_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenSpecialNonWorkingOt_EmitsSpecialHolidayOt()
    {
        var engine = BuildEngine(HolidayType.SPECIAL_NON_WORKING, holidayPayEnabled: true);
        var schedule = StandardShift();
        var lateOut = ShiftEnd.AddHours(2);
        var logs = new[] { InLog(ShiftStart), OutLog(lateOut) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.SPECIAL_HOLIDAY_OT);
    }

    [Fact]
    public void Compute_WhenSpecialNonWorkingEarlyOt_EmitsSpecialHolidayEarlyOt()
    {
        var engine = BuildEngine(HolidayType.SPECIAL_NON_WORKING, holidayPayEnabled: true);
        var schedule = StandardShift();
        var earlyIn = ShiftStart.AddHours(-1);
        var logs = new[] { InLog(earlyIn), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.SPECIAL_HOLIDAY_EARLY_OT);
    }

    [Fact]
    public void Compute_WhenNotSpecialHoliday_DoesNotEmitSpecialHolidayPaidHours()
    {
        var engine = BuildEngine(HolidayType.NONE, holidayPayEnabled: true);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.DoesNotContain(result.Segments, s => s.Classification == TimeSegmentClassification.SPECIAL_HOLIDAY_PAID_HOURS);
    }
}
