using NSubstitute;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Domain.Services;
using Xunit;

namespace PhPayrollTimeApi.Domain.Tests.LogPairing;

public class LogPairingAlgorithmTests
{
    // Manila offset
    private static readonly TimeSpan MHours = TimeSpan.FromHours(8);

    // 2026-05-06 08:00 Manila = 2026-05-06 00:00 UTC
    private static readonly DateTimeOffset ShiftStartUtc = new(2026, 5, 6, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ShiftEndUtc   = new(2026, 5, 6, 9, 0, 0, TimeSpan.Zero);

    private static ShiftSchedule MakeSchedule() => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid(),
        ScheduleStart = ShiftStartUtc,
        ScheduleEnd   = ShiftEndUtc,
        IsActive = true
    };

    private static TimeLog Log(LogType type, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = Guid.NewGuid(),
        LogType = type,
        LoggedAt = at
    };

    private static LogPairingService BuildService(DateTimeOffset? now = null)
    {
        var clock = Substitute.For<IClockProvider>();
        clock.UtcNow.Returns(now ?? ShiftEndUtc.AddHours(1));

        var tracker = new SimpleTracker();
        return new LogPairingService(clock, tracker);
    }

    [Fact]
    public void PairLogs_WhenInAndOutOnSameDay_ReturnsPair()
    {
        var svc = BuildService();
        var schedule = MakeSchedule();
        var inLog  = Log(LogType.IN,  ShiftStartUtc);
        var outLog = Log(LogType.OUT, ShiftEndUtc);

        var (inResult, outResult) = svc.PairLogs(schedule, new[] { inLog, outLog });

        Assert.Equal(inLog.Id, inResult!.Id);
        Assert.Equal(outLog.Id, outResult!.Id);
    }

    [Fact]
    public void PairLogs_WhenNoInLog_ReturnsNullPair()
    {
        var svc = BuildService();
        var schedule = MakeSchedule();
        var outLog = Log(LogType.OUT, ShiftEndUtc);

        var (inResult, outResult) = svc.PairLogs(schedule, new[] { outLog });

        Assert.Null(inResult);
        Assert.Null(outResult);
    }

    [Fact]
    public void PairLogs_WhenInLogFoundButNoOut_ReturnsNullOut()
    {
        var svc = BuildService();
        var schedule = MakeSchedule();
        var inLog = Log(LogType.IN, ShiftStartUtc);

        var (inResult, outResult) = svc.PairLogs(schedule, new[] { inLog });

        Assert.NotNull(inResult);
        Assert.Null(outResult);
    }

    [Fact]
    public void PairLogs_WhenInLogFromPriorDay_ReturnsIt()
    {
        var svc = BuildService();
        var schedule = MakeSchedule();
        // In log from 10pm Manila prev day = 14:00 UTC prev day
        var prevDayIn  = new DateTimeOffset(2026, 5, 5, 14, 0, 0, TimeSpan.Zero);
        var outLog     = Log(LogType.OUT, ShiftEndUtc);
        var inLog      = Log(LogType.IN, prevDayIn);

        var (inResult, outResult) = svc.PairLogs(schedule, new[] { inLog, outLog });

        Assert.Equal(inLog.Id, inResult!.Id);
        Assert.Equal(outLog.Id, outResult!.Id);
    }

    [Fact]
    public void PairLogs_WhenInLogTwoDaysPrior_WithDefaultLookback_NotFound()
    {
        var svc = BuildService();
        var schedule = MakeSchedule();
        // In log from 2 days prior — beyond default lookback of 1 day
        var twoDayPriorIn = new DateTimeOffset(2026, 5, 4, 14, 0, 0, TimeSpan.Zero);
        var outLog = Log(LogType.OUT, ShiftEndUtc);
        var inLog  = Log(LogType.IN, twoDayPriorIn);

        var (inResult, _) = svc.PairLogs(schedule, new[] { inLog, outLog });

        Assert.Null(inResult);
    }

    [Fact]
    public void PairLogs_WhenSameInLogCalledTwice_SecondCallGetsDifferentLog()
    {
        // Two schedules on same day — first schedule claims the IN log
        var clock = Substitute.For<IClockProvider>();
        clock.UtcNow.Returns(ShiftEndUtc.AddHours(2));
        var tracker = new SimpleTracker();
        var svc = new LogPairingService(clock, tracker);

        var schedule1 = MakeSchedule();
        var schedule2 = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            EmployeeId = schedule1.EmployeeId,
            ScheduleStart = ShiftEndUtc,
            ScheduleEnd   = ShiftEndUtc.AddHours(9),
            IsActive = true
        };

        var inLog1 = Log(LogType.IN, ShiftStartUtc);
        var inLog2 = Log(LogType.IN, ShiftEndUtc);
        var outLog1 = Log(LogType.OUT, ShiftEndUtc.AddMinutes(-30));
        var all = new[] { inLog1, inLog2, outLog1 };

        var (in1, _) = svc.PairLogs(schedule1, all);
        var (in2, _) = svc.PairLogs(schedule2, all);

        Assert.Equal(inLog1.Id, in1!.Id);
        Assert.Equal(inLog2.Id, in2!.Id); // different IN claimed
    }

    private class SimpleTracker : ILogClaimTracker
    {
        private readonly HashSet<Guid> _claimed = new();
        public void Claim(Guid id) => _claimed.Add(id);
        public bool IsClaimed(Guid id) => _claimed.Contains(id);
    }
}
