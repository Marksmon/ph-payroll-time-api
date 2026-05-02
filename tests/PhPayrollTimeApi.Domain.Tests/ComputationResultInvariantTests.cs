using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.ValueObjects;
using Xunit;

namespace PhPayrollTimeApi.Domain.Tests;

public class ComputationResultInvariantTests
{
    private static readonly DateTimeOffset Base = new(2026, 5, 1, 8, 0, 0, TimeSpan.FromHours(8));

    [Fact]
    public void Constructor_WhenSegmentsValid_CreatesResult()
    {
        var segments = new List<TimeSegment>
        {
            new(Base, Base.AddHours(4), 240, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
            new(Base.AddHours(4), Base.AddHours(8), 240, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };

        var result = new ComputationResult(
            Guid.NewGuid(), Guid.NewGuid(),
            Base, Base.AddHours(9),
            Base.AddMinutes(-30), Base.AddHours(9),
            60, segments, false);

        Assert.Equal(2, result.Segments.Count);
    }

    [Fact]
    public void Constructor_WhenZeroDurationSegment_ThrowsComputationInvariantException()
    {
        var segments = new List<TimeSegment>
        {
            new(Base, Base, 0, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
        };

        var ex = Assert.Throws<ComputationInvariantException>(() =>
            new ComputationResult(Guid.NewGuid(), Guid.NewGuid(),
                Base, Base.AddHours(9), null, null, 0, segments, false));

        Assert.Contains("Invariant 3", ex.Message);
    }

    [Fact]
    public void Constructor_WhenOverlappingSegments_ThrowsComputationInvariantException()
    {
        var segments = new List<TimeSegment>
        {
            new(Base, Base.AddHours(5), 300, TimeSegmentClassification.NORMAL_PAID_HOURS, ApprovalStatus.APPROVED),
            new(Base.AddHours(4), Base.AddHours(9), 300, TimeSegmentClassification.NORMAL_OT, ApprovalStatus.PENDING),
        };

        var ex = Assert.Throws<ComputationInvariantException>(() =>
            new ComputationResult(Guid.NewGuid(), Guid.NewGuid(),
                Base, Base.AddHours(9), null, null, 0, segments, false));

        Assert.Contains("Invariant 2", ex.Message);
    }
}
