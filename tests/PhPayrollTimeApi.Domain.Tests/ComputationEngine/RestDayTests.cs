using PhPayrollTimeApi.Domain.Enums;
using Xunit;
using static PhPayrollTimeApi.Domain.Tests.ComputationEngine.ComputationTestHelper;

namespace PhPayrollTimeApi.Domain.Tests.ComputationEngine;

public class RestDayTests
{
    // ManilaMidnight is 2026-05-06 Wednesday; rest day on Wednesday = DayOfWeek.Wednesday
    private static readonly DateTimeOffset ShiftStart = ManilaMidnight.AddHours(8).ToUniversalTime();
    private static readonly DateTimeOffset ShiftEnd   = ManilaMidnight.AddHours(17).ToUniversalTime();

    [Fact]
    public void Compute_WhenRestDay_EmitsRestDayPaidHours()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var pattern = RestDayPattern(DayOfWeek.Wednesday); // 2026-05-06 is Wednesday
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, pattern, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.REST_DAY_PAID_HOURS);
        Assert.DoesNotContain(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenNotRestDay_DoesNotEmitRestDayPaidHours()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var pattern = RestDayPattern(DayOfWeek.Sunday); // Not Wednesday
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, pattern, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.DoesNotContain(result.Segments, s => s.Classification == TimeSegmentClassification.REST_DAY_PAID_HOURS);
        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.NORMAL_PAID_HOURS);
    }

    [Fact]
    public void Compute_WhenRestDayOt_EmitsRestDayOt()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var pattern = RestDayPattern(DayOfWeek.Wednesday);
        var lateOut = ShiftEnd.AddHours(2);
        var logs = new[] { InLog(ShiftStart), OutLog(lateOut) };

        var result = engine.ComputeAsync(schedule, pattern, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.REST_DAY_OT);
    }

    [Fact]
    public void Compute_WhenRestDayEarlyOt_EmitsRestDayEarlyOt()
    {
        var engine = BuildEngine();
        var schedule = StandardShift();
        var pattern = RestDayPattern(DayOfWeek.Wednesday);
        var earlyIn = ShiftStart.AddHours(-1);
        var logs = new[] { InLog(earlyIn), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, pattern, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s => s.Classification == TimeSegmentClassification.REST_DAY_EARLY_OT);
    }

    [Fact]
    public void Compute_WhenRestDayPlusRegularHoliday_EmitsRestDayRegularHolidayPaidPremium()
    {
        var engine = BuildEngine(HolidayType.REGULAR);
        var schedule = StandardShift();
        var pattern = RestDayPattern(DayOfWeek.Wednesday);
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, pattern, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s =>
            s.Classification == TimeSegmentClassification.REST_DAY_REGULAR_HOLIDAY_PAID_PREMIUM);
    }

    [Fact]
    public void Compute_WhenRestDayPlusSpecialNonWorking_EmitsRestDaySpecialHolidayPaidPremium()
    {
        var engine = BuildEngine(HolidayType.SPECIAL_NON_WORKING);
        var schedule = StandardShift();
        var pattern = RestDayPattern(DayOfWeek.Wednesday);
        var logs = new[] { InLog(ShiftStart), OutLog(ShiftEnd) };

        var result = engine.ComputeAsync(schedule, pattern, logs, CancellationToken.None).GetAwaiter().GetResult();

        Assert.Contains(result.Segments, s =>
            s.Classification == TimeSegmentClassification.REST_DAY_SPECIAL_HOLIDAY_PAID_PREMIUM);
    }
}
