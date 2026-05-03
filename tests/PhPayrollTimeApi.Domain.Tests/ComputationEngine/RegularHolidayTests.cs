using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using Xunit;
using static PhPayrollTimeApi.Domain.Tests.ComputationEngine.ComputationTestHelper;

namespace PhPayrollTimeApi.Domain.Tests.ComputationEngine;

public class RegularHolidayTests
{
    private static readonly DateTimeOffset ShiftStart = ManilaMidnight.AddHours(8).ToUniversalTime();
    private static readonly DateTimeOffset ShiftEnd   = ManilaMidnight.AddHours(17).ToUniversalTime();

    [Fact]
    public void Compute_WhenRegularHolidayNoLogsNoApproval_EmitsRegularHolidayRestPaid()
    {
        var engine = BuildEngine(HolidayType.REGULAR);
        var schedule = StandardShift();

        var result = engine.ComputeAsync(schedule, null, Array.Empty<TimeLog>(), CancellationToken.None).GetAwaiter().GetResult();

        Assert.Single(result.Segments);
        Assert.Equal(TimeSegmentClassification.REGULAR_HOLIDAY_REST_PAID, result.Segments[0].Classification);
    }

    [Fact]
    public void Compute_WhenRegularHolidayWithLogs_EmitsRegularHolidayPaidHours()
    {
        var engine = BuildEngine(HolidayType.REGULAR, holidayPayEnabled: true);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.REGULAR_HOLIDAY_PAID_HOURS);
        Assert.True(result.HrReviewFlagged);
    }

    [Fact]
    public void Compute_WhenRegularHolidayFlagDisabled_NoPaidHoursSegment()
    {
        var engine = BuildEngine(HolidayType.REGULAR, holidayPayEnabled: false);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.DoesNotContain(result.Segments, s =>
            s.Classification == TimeSegmentClassification.REGULAR_HOLIDAY_PAID_HOURS ||
            s.Classification == TimeSegmentClassification.NIGHT_DIFF_REGULAR_HOLIDAY_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenRegularHolidayOt_EmitsRegularHolidayOt()
    {
        var engine = BuildEngine(HolidayType.REGULAR, holidayPayEnabled: true);
        var schedule = StandardShift();
        var lateOut = ShiftEnd.AddHours(2);
        var logs = new[] { InLog(ShiftStart), OutLog(lateOut) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.REGULAR_HOLIDAY_OT);
    }

    [Fact]
    public void Compute_WhenRegularHolidayEarlyOt_EmitsRegularHolidayEarlyOt()
    {
        var engine = BuildEngine(HolidayType.REGULAR, holidayPayEnabled: true);
        var schedule = StandardShift();
        var earlyIn = ShiftStart.AddHours(-1);
        var logs = new[] { InLog(earlyIn), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.REGULAR_HOLIDAY_EARLY_OT);
    }

    [Fact]
    public void Compute_WhenSpecialWorkingHoliday_TreatedAsOrdinaryDay()
    {
        var engine = BuildEngine(HolidayType.SPECIAL_WORKING, holidayPayEnabled: true);
        var schedule = StandardShift();
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, null, logs, CancellationToken.None).GetAwaiter().GetResult();

        // Should emit NORMAL_PAID_HOURS, not holiday variants
        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
        Assert.DoesNotContain(result.Segments, s =>
            s.Classification == TimeSegmentClassification.REGULAR_HOLIDAY_PAID_HOURS ||
            s.Classification == TimeSegmentClassification.SPECIAL_HOLIDAY_PAID_HOURS);
    }
}
