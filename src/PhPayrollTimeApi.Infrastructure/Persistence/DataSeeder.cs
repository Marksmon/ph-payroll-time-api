using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Infrastructure.Persistence;

public static class DataSeeder
{
    public static readonly Guid EmployeeId1 = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid EmployeeId2 = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid EmployeeId3 = new("00000000-0000-0000-0000-000000000003");

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Employees.AnyAsync(cancellationToken: ct))
            return;

        var seedYear = DateTimeOffset.UtcNow.Year;

        db.Employees.AddRange(
            new Employee
            {
                Id = EmployeeId1, EmployeeNumber = "EMP-001", FullName = "Juan dela Cruz",
                Role = UserRole.EMPLOYEE, JwtSubjectClaim = "emp-001", IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            },
            new Employee
            {
                Id = EmployeeId2, EmployeeNumber = "MGR-001", FullName = "Maria Santos",
                Role = UserRole.MANAGER, JwtSubjectClaim = "mgr-001", IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            },
            new Employee
            {
                Id = EmployeeId3, EmployeeNumber = "HR-001", FullName = "Pedro Reyes",
                Role = UserRole.HR_ADMIN, JwtSubjectClaim = "hr-001", IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            }
        );

        db.HolidayCalendarEntries.AddRange(
            new HolidayCalendarEntry
            {
                Id = Guid.NewGuid(), Date = new DateOnly(seedYear, 1, 1),
                Name = "New Year's Day", Type = HolidayType.REGULAR,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            },
            new HolidayCalendarEntry
            {
                Id = Guid.NewGuid(), Date = new DateOnly(seedYear, 11, 2),
                Name = "All Souls' Day", Type = HolidayType.SPECIAL_NON_WORKING,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            }
        );

        // Normal daytime shift: 8am–5pm Asia/Manila
        var scheduleStart = new DateTimeOffset(seedYear, 5, 1, 8, 0, 0, TimeSpan.FromHours(8));
        db.ShiftSchedules.Add(new ShiftSchedule
        {
            Id = Guid.NewGuid(), EmployeeId = EmployeeId1,
            ScheduleStart = scheduleStart,
            ScheduleEnd = scheduleStart.AddHours(9),
            BreakWindows = new List<BreakWindow>
            {
                new() { BreakStart = scheduleStart.AddHours(4), BreakEnd = scheduleStart.AddHours(5) }
            },
            IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        // Night shift crossing midnight: 10pm–6am Asia/Manila
        var nightStart = new DateTimeOffset(seedYear, 5, 2, 22, 0, 0, TimeSpan.FromHours(8));
        db.ShiftSchedules.Add(new ShiftSchedule
        {
            Id = Guid.NewGuid(), EmployeeId = EmployeeId1,
            ScheduleStart = nightStart,
            ScheduleEnd = nightStart.AddHours(8),
            BreakWindows = new List<BreakWindow>(),
            IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        // Time logs: Journey 1 — normal day IN/OUT
        db.TimeLogs.AddRange(
            new TimeLog
            {
                Id = Guid.NewGuid(), EmployeeId = EmployeeId1,
                LogType = LogType.IN, LoggedAt = scheduleStart.AddMinutes(-15),
                Source = "BIOMETRIC", CreatedAt = DateTimeOffset.UtcNow
            },
            new TimeLog
            {
                Id = Guid.NewGuid(), EmployeeId = EmployeeId1,
                LogType = LogType.OUT, LoggedAt = scheduleStart.AddHours(9).AddMinutes(5),
                Source = "BIOMETRIC", CreatedAt = DateTimeOffset.UtcNow
            }
        );

        await db.SaveChangesAsync(ct);
    }
}
