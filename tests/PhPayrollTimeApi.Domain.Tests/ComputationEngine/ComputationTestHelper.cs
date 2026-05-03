using NSubstitute;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;
using CE = PhPayrollTimeApi.Domain.Services.ComputationEngine;

namespace PhPayrollTimeApi.Domain.Tests.ComputationEngine;

internal static class ComputationTestHelper
{
    // Manila midnight on a known weekday (Wednesday 2026-05-06)
    public static readonly DateTimeOffset ManilaMidnight =
        new(2026, 5, 6, 0, 0, 0, TimeSpan.FromHours(8));

    // Standard 8-hour shift: 8am–5pm Manila
    public static ShiftSchedule StandardShift(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        EmployeeId = Guid.NewGuid(),
        ScheduleStart = ManilaMidnight.AddHours(8).ToUniversalTime(),  // 00:00 UTC
        ScheduleEnd   = ManilaMidnight.AddHours(17).ToUniversalTime(), // 09:00 UTC
        BreakWindows  = new List<BreakWindow>(),
        IsActive = true
    };

    public static ShiftSchedule ShiftWithBreak(DateTimeOffset breakStart, DateTimeOffset breakEnd)
    {
        var s = StandardShift();
        s.BreakWindows.Add(new BreakWindow { BreakStart = breakStart, BreakEnd = breakEnd });
        return s;
    }

    public static WorkSchedulePattern RestDayPattern(DayOfWeek restDay)
    {
        var pattern = new WorkSchedulePattern
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            EffectiveDate = new DateOnly(2026, 1, 1),
            ExpiryDate = null,
            IsActive = true
        };
        pattern.RestDays.Add(restDay);
        return pattern;
    }

    public static TimeLog InLog(DateTimeOffset at, Guid? empId = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = empId ?? Guid.NewGuid(),
        LogType = LogType.IN,
        LoggedAt = at
    };

    public static TimeLog OutLog(DateTimeOffset at, Guid? empId = null) => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = empId ?? Guid.NewGuid(),
        LogType = LogType.OUT,
        LoggedAt = at
    };

    public static CE BuildEngine(
        HolidayType holidayType = HolidayType.NONE,
        DateTimeOffset? now = null,
        bool holidayApproved = false,
        bool restDayApproved = false,
        bool holidayPayEnabled = true)
    {
        var calendar = Substitute.For<IHolidayCalendar>();
        calendar.GetHolidayTypeAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(holidayType);

        var clock = Substitute.For<IClockProvider>();
        clock.UtcNow.Returns(now ?? ManilaMidnight.AddHours(20).ToUniversalTime());

        var flags = Substitute.For<IFeatureFlagProvider>();
        flags.IsEnabledAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(holidayPayEnabled);

        var tracker = new InMemoryClaimTracker();

        var approvalRepo = Substitute.For<IHolidayApprovalRepository>();
        approvalRepo.GetHolidayScheduleApprovalAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(holidayApproved
                ? new HolidayScheduleApproval { Status = ApprovalStatus.APPROVED }
                : (HolidayScheduleApproval?)null);
        approvalRepo.GetRestDayScheduleApprovalAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(restDayApproved
                ? new RestDayScheduleApproval { Status = ApprovalStatus.APPROVED }
                : (RestDayScheduleApproval?)null);

        return new CE(calendar, clock, flags, tracker, approvalRepo);
    }

    // Simple in-memory ILogClaimTracker for tests (avoids DI)
    private class InMemoryClaimTracker : ILogClaimTracker
    {
        private readonly HashSet<Guid> _claimed = new();
        public void Claim(Guid id) => _claimed.Add(id);
        public bool IsClaimed(Guid id) => _claimed.Contains(id);
    }
}
