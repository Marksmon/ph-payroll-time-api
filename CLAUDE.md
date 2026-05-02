# CLAUDE.md — ph-payroll-time-api

Philippine DOLE RA 6727 overtime-rules REST API. .NET 8, PostgreSQL 17, Clean Architecture, CQRS.

---

## Solution Layout

```
src/
  PhPayrollTimeApi.Domain           # Entities, Enums, Exceptions, Interfaces (no deps)
  PhPayrollTimeApi.Application      # CQRS handlers, abstractions (depends on Domain)
  PhPayrollTimeApi.Infrastructure   # EF Core, Npgsql, repositories (depends on Domain+Application)
  PhPayrollTimeApi.Api              # ASP.NET Core, controllers, middleware (depends on all)
tests/
  PhPayrollTimeApi.Unit.Tests       # Domain logic, computation engine
  PhPayrollTimeApi.Integration.Tests # WebApplicationFactory, real PostgreSQL
```

---

## Key Commands

```powershell
# Build
dotnet build ph-payroll-time-api.sln

# Run (from repo root, uses appsettings.Development.json)
dotnet run --project src/PhPayrollTimeApi.Api/PhPayrollTimeApi.Api.csproj

# Tests (requires local PostgreSQL 17 running)
dotnet test ph-payroll-time-api.sln

# EF Migrations (run from repo root; --project = Infrastructure, --startup-project = Api)
dotnet ef migrations add <Name> --project src/PhPayrollTimeApi.Infrastructure --startup-project src/PhPayrollTimeApi.Api
dotnet ef database update --project src/PhPayrollTimeApi.Infrastructure --startup-project src/PhPayrollTimeApi.Api
```

---

## Architecture Patterns

### CQRS
- **Commands:** `ICommandHandler<TCommand>` → `Task HandleAsync(TCommand, CancellationToken)`
- **Queries:** `IQueryHandler<TQuery, TResult>` → `Task<TResult> HandleAsync(TQuery, CancellationToken)`
- Auto-registered by `AddApplicationServices()` (assembly scan in `Api/Extensions/ApplicationServiceExtensions.cs`)
- Commands live in `Application/Commands/`, Queries in `Application/Queries/`
- Controllers inject handlers directly; no mediator package

### Controllers
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
public class EmployeesController : ControllerBase { ... }
```

### Role-based Auth
- Roles: `EMPLOYEE`, `MANAGER`, `HR_ADMIN` (stored as string in DB)
- JWT claim: `role` (verbatim — `MapInboundClaims = false` set in Program.cs)
- Use `[Authorize(Roles = "MANAGER,HR_ADMIN")]` or check `User.FindFirstValue("role")` for inline logic
- The `sub` claim identifies the employee's `JwtSubjectClaim` field

### Error Handling (RFC 7807)
- All errors → `application/problem+json`
- `GlobalExceptionHandler : IExceptionHandler` catches domain exceptions → maps to status codes
- `EntityNotFoundException` → 404, `DomainValidationException` → 422, `ScheduleOverlapException` → 409
- **Critical:** Always pass `contentType: "application/problem+json"` to `WriteAsJsonAsync()` — the default overload resets the Content-Type header
- Model validation errors use `ContentResult` (not `ObjectResult`/`BadRequestObjectResult`) to preserve `application/problem+json`

### Idempotency
- Middleware in `Api/Middleware/IdempotencyMiddleware.cs`
- `IdempotencyMiddleware.ComputeCacheKey()` is `public static` (required by integration tests)

---

## Critical Constraints

### DateTimeOffset — UTC Only (Npgsql 8)
- **Npgsql 8 rejects `DateTimeOffset` values with non-UTC offsets** for `timestamptz` columns
- Always store and pass UTC: `new DateTimeOffset(year, m, d, h, min, s, TimeSpan.Zero)`
- PHT (UTC+8) times must be converted to UTC before storing: 8am PHT = 0:00 UTC, 10pm PHT = 14:00 UTC
- Global EF convention in `AppDbContext.ConfigureConventions()` maps all `DateTimeOffset` → `timestamptz`

### EF Core Conventions (EF Core 6+)
- Global property conventions go in `ConfigureConventions(ModelConfigurationBuilder)`, NOT `OnModelCreating`
- Entity-specific config in `Infrastructure/Persistence/Configurations/<Entity>Configuration.cs` implementing `IEntityTypeConfiguration<T>`
- All columns use `snake_case`; `Role` stored as `HasConversion<string>()`

### JWT
- RS256 only — `ValidAlgorithms = ["RS256"]` rejects `alg:none` and HS256
- Keys: `jwt-public.pem` / `jwt-private.pem` (path from `appsettings.json` `Jwt:PublicKeyPath` / `Jwt:PrivateKeyPath`)
- `KeyManagement.EnsureKeysExist()` auto-generates keys in Development if missing
- `MapInboundClaims = false` — preserves `sub` and `role` verbatim (do not remove)

---

## Database

- PostgreSQL 17, localhost:5432
- Connection string: `appsettings.Development.json` → `ConnectionStrings:DefaultConnection`
- DB name: `ph_payroll_time` (Development)
- Migrations: `Infrastructure/Migrations/`
- DataSeeder runs on startup in Development (non-fatal if DB unreachable)
- Seed employees: `emp-001` (EMPLOYEE), `mgr-001` (MANAGER), `hr-001` (HR_ADMIN)

---

## Integration Tests

```csharp
// Test class pattern
public class EmployeesControllerTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    public EmployeesControllerTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PostEmployee_AsManager_Returns201()
    {
        var client = _fixture.CreateClient();
        var token = _fixture.GenerateTestToken(sub: "mgr-001", role: "MANAGER");
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        // ...
    }
}
```

- `ApiTestFixture` in `tests/.../Fixtures/ApiTestFixture.cs`
- `GenerateTestToken(sub, role, expired, issuer, audience, algorithm)` — all optional except sub/role
- RSA keys are written to temp directory in static constructor; `UseSetting()` overrides `Jwt:PublicKeyPath`
- Test DB: same PostgreSQL 17 — tests use real DB (no mocking — past incidents with mock/prod divergence)
- `ErrorProbeController` (`[AllowAnonymous]`) triggers specific exceptions for exception handler tests

---

## Domain Entities (brief)

| Entity | Key Fields |
|--------|-----------|
| `Employee` | `Id`, `EmployeeNumber`, `FullName`, `Role`, `JwtSubjectClaim`, `IsActive` |
| `ShiftSchedule` | `EmployeeId`, `ScheduleStart`, `ScheduleEnd`, `BreakWindows`, `IsActive` |
| `WorkSchedulePattern` | `EmployeeId`, `RestDays (int[])`, `EffectiveDate`, `ExpiryDate` |
| `TimeLog` | `EmployeeId`, `LogType (IN/OUT)`, `LoggedAt`, `Source` |
| `HolidayCalendarEntry` | `Date`, `Name`, `Type (REGULAR/SPECIAL_NON_WORKING)` |

Soft-delete on `Employee`: set `IsActive = false`, never hard-delete (FK integrity with `TimeLog`).

---

## BMad Workflow

- Planning artifacts: `_bmad-output/planning-artifacts/` (prd.md, architecture.md, epics.md)
- Story files: `_bmad-output/implementation-artifacts/<story-key>.md`
- Sprint status: `_bmad-output/implementation-artifacts/sprint-status.yaml`
- Story key format: `{epic}-{story}-{kebab-title}` e.g. `2-1-create-and-update-employee-profile`
- Skills: `/bmad-create-story`, `/bmad-dev-story`, `/bmad-sprint-planning`

---

## Package Versions (pinned)

| Package | Version |
|---------|---------|
| Microsoft.EntityFrameworkCore.* | 8.0.25 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.25 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.25 |
| Microsoft.EntityFrameworkCore.Relational | 8.0.25 (explicitly pinned — JwtBearer pulls older version) |
| Swashbuckle.AspNetCore | 6.9.0 |
| Asp.Versioning.Mvc | 8.1.0 |
| Serilog.AspNetCore | 8.x |
