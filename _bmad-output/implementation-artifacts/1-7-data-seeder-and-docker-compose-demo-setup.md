# Story 1.7: Data Seeder & Docker Compose Demo Setup

Status: review

## Story

As a portfolio reviewer / developer,
I want `docker compose up --build` to start the full stack with all demo data seeded and Swagger accessible,
So that the portfolio demo runs end-to-end without any manual configuration.

## Acceptance Criteria

1. **Given** the Docker Compose file **When** `docker compose up --build` is run **Then** two services start: `postgres:16-alpine` and the built API image **And** the API is reachable with Swagger UI accessible

2. **Given** the application starts in Development environment **When** `DataSeeder` runs **Then** 3 employees (Employee/Manager/HR Admin roles) seeded **And** 1 Regular Holiday + 1 Special Non-Working Holiday seeded **And** sample shift schedules spanning midnight and 10pm night-diff boundary seeded **And** sample time logs for Journey 1–8 scenarios seeded

3. **Given** `HardcodedFeatureFlagProvider` **When** any feature flag is evaluated **Then** all flags return `true` (already done in Story 1.1 — verify)

## Tasks / Subtasks

- [x] **Task 1: Create DataSeeder** (AC: 2)
  - [x] Create `src/PhPayrollTimeApi.Infrastructure/Persistence/DataSeeder.cs`
  - [x] `SeedAsync(AppDbContext db, CancellationToken ct)` static method
  - [x] Seed 3 employees with fixed Guid IDs and `JwtSubjectClaim` values usable with test token generator
  - [x] Seed 1 Regular Holiday (Jan 1 New Year) + 1 Special Non-Working Holiday (sample)
  - [x] Seed 2 shift schedules: normal daytime + night-shift crossing midnight
  - [x] Seed 4 time logs: IN/OUT pairs for Journey 1 (normal day) and Journey 2 (night diff)
  - [x] Guard: skip seeding if data already exists (`db.Employees.AnyAsync()`)

- [x] **Task 2: Wire DataSeeder into Program.cs (Dev only)** (AC: 2)
  - [x] After `app.Build()`, call `DataSeeder.SeedAsync(...)` in Development environment only
  - [x] Use `app.Services.CreateScope()` to get scoped `AppDbContext`

- [x] **Task 3: Update docker-compose.yml for key generation** (AC: 1)
  - [x] Add `Jwt__PublicKeyPath` and `Jwt__PrivateKeyPath` environment variables so Docker container generates keys on first startup
  - [x] Add `volumes` mount for `keys/` directory so generated keys persist across restarts

## Dev Notes

### DataSeeder

```csharp
// src/PhPayrollTimeApi.Infrastructure/Persistence/DataSeeder.cs
public static class DataSeeder
{
    // Fixed seed GUIDs — deterministic for demo/test reproducibility
    public static readonly Guid EmployeeId1 = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid EmployeeId2 = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid EmployeeId3 = new("00000000-0000-0000-0000-000000000003");

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Employees.AnyAsync(cancellationToken: ct))
            return;

        // Employees
        db.Employees.AddRange(
            new Employee { Id = EmployeeId1, EmployeeNumber = "EMP-001", FullName = "Juan dela Cruz",
                Role = UserRole.EMPLOYEE, JwtSubjectClaim = "emp-001", IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Employee { Id = EmployeeId2, EmployeeNumber = "MGR-001", FullName = "Maria Santos",
                Role = UserRole.MANAGER, JwtSubjectClaim = "mgr-001", IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Employee { Id = EmployeeId3, EmployeeNumber = "HR-001", FullName = "Pedro Reyes",
                Role = UserRole.HR_ADMIN, JwtSubjectClaim = "hr-001", IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );

        // Holidays
        var seedYear = DateTimeOffset.UtcNow.Year;
        db.HolidayCalendarEntries.AddRange(
            new HolidayCalendarEntry { Id = Guid.NewGuid(),
                Date = new DateOnly(seedYear, 1, 1), Name = "New Year's Day",
                Type = HolidayType.REGULAR,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new HolidayCalendarEntry { Id = Guid.NewGuid(),
                Date = new DateOnly(seedYear, 11, 2), Name = "All Souls' Day",
                Type = HolidayType.SPECIAL_NON_WORKING,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );

        // Shift schedule — normal 8am-5pm
        var scheduleStart = new DateTimeOffset(seedYear, 5, 1, 8, 0, 0, TimeSpan.FromHours(8));
        var shift1Id = Guid.NewGuid();
        db.ShiftSchedules.Add(new ShiftSchedule
        {
            Id = shift1Id, EmployeeId = EmployeeId1,
            ScheduleStart = scheduleStart,
            ScheduleEnd = scheduleStart.AddHours(9),
            BreakWindows = new List<BreakWindow>
            {
                new() { BreakStart = scheduleStart.AddHours(4), BreakEnd = scheduleStart.AddHours(5) }
            },
            IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        // Shift schedule — night shift crossing midnight (10pm–6am)
        var nightStart = new DateTimeOffset(seedYear, 5, 2, 22, 0, 0, TimeSpan.FromHours(8));
        db.ShiftSchedules.Add(new ShiftSchedule
        {
            Id = Guid.NewGuid(), EmployeeId = EmployeeId1,
            ScheduleStart = nightStart,
            ScheduleEnd = nightStart.AddHours(8),
            BreakWindows = new List<BreakWindow>(),
            IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });

        // Time logs — Journey 1 (normal day IN/OUT)
        db.TimeLogs.AddRange(
            new TimeLog { Id = Guid.NewGuid(), EmployeeId = EmployeeId1,
                LogType = LogType.IN, LoggedAt = scheduleStart.AddMinutes(-15),
                Source = "BIOMETRIC", CreatedAt = DateTimeOffset.UtcNow },
            new TimeLog { Id = Guid.NewGuid(), EmployeeId = EmployeeId1,
                LogType = LogType.OUT, LoggedAt = scheduleStart.AddHours(9).AddMinutes(5),
                Source = "BIOMETRIC", CreatedAt = DateTimeOffset.UtcNow }
        );

        await db.SaveChangesAsync(ct);
    }
}
```

### Program.cs Seeder Call (after app.Build())

```csharp
// Seed demo data in Development only
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DataSeeder.SeedAsync(db);
}
```

Add `await` — Program.cs must be `async` or use `.GetAwaiter().GetResult()`. The ASP.NET Core 8 top-level program supports `await` directly.

### Docker Compose Key Management

The API generates keys on startup (Dev environment) but in Docker the `keys/` directory is inside the container. Add a named volume:

```yaml
api:
  volumes:
    - api_keys:/app/keys
  environment:
    - Jwt__PublicKeyPath=/app/keys/jwt-public.pem
    - Jwt__PrivateKeyPath=/app/keys/jwt-private.pem

volumes:
  api_keys:
```

This persists the generated key pair across `docker compose up` restarts.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- DataSeeder: 3 employees (emp-001/mgr-001/hr-001), 2 holidays, 2 shift schedules (day + night), 2 time logs for Journey 1
- Program.cs: `await DataSeeder.SeedAsync(db)` called after app.Build() in Development
- docker-compose.yml: added `api_keys` named volume + `Jwt__PublicKeyPath`/`Jwt__PrivateKeyPath` env vars for Docker key persistence

### File List

- src/PhPayrollTimeApi.Infrastructure/Persistence/DataSeeder.cs (new)
- src/PhPayrollTimeApi.Api/Program.cs (updated)
- docker-compose.yml (updated)
