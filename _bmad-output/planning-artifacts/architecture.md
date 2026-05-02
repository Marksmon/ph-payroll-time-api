---
stepsCompleted: [step-01-init, step-02-context, step-03-starter, step-04-decisions, step-05-patterns, step-06-structure, step-07-validation, step-08-complete]
lastStep: 8
status: 'complete'
completedAt: '2026-05-02'
inputDocuments: ['_bmad-output/planning-artifacts/prd.md']
workflowType: 'architecture'
project_name: 'ph-payroll-time-api'
user_name: 'Mark'
date: '2026-05-02'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements (56 FRs across 8 areas):**

| Area | FRs | Architectural Weight |
|---|---|---|
| Employee & Schedule Management | FR1–FR8 | Moderate — CRUD + overlap validation |
| Time Log Recording | FR9–FR13 | Low-moderate — idempotency, role-gated log submission |
| Overtime Computation Engine | FR14–FR25 | **High** — stateless, deterministic, 40 types, 8 invariants |
| Approval Workflow | FR26–FR38 | **High** — staged actions, atomic commit, cascades, audit |
| Holiday Calendar Management | FR39–FR42 | Low — CRUD + stale-approval flagging side effect |
| Attendance Reporting | FR43–FR46 | Moderate — bulk-fetch strategy, role-scoped |
| System Configuration & Feature Flags | FR47–FR49 | Low — toggle + immutable audit log |
| API Platform, Security & Contracts | FR50–FR56 | **High** — JWT RS256, rate limiting, versioning, RFC 7807, Swagger |

**Non-Functional Requirements:**

- **Performance:** Computation < 200ms (p95); attendance report 31 days × 100 employees < 5s; bulk approval commit < 3s; all other endpoints < 500ms
- **Security:** HTTPS-only; JWT RS256, `alg: none` rejected; `exp`/`iss`/`aud` validated; role-scoped access from claims only; rate limiting per `sub`; parameterized queries throughout
- **Reliability:** Atomic OT commits (full transaction or rollback); audit log written in same transaction; deterministic computation (same inputs → same outputs); idempotency within 5-minute window; NTP-synchronized clock
- **Testability:** All 40 types have positive + negative tests; 8 invariants have dedicated violation tests; 8 user journeys as integration tests; computation engine testable with no DB, HTTP, or real clock

**Scale & Complexity:**

- Primary domain: Internal REST API — HR / Time & Attendance
- Complexity level: Medium (infrastructure simple; domain logic high-complexity)
- Estimated architectural components: ~6 domain entities, ~8 CQRS command handlers, ~6 query handlers, ~28 API routes, 5 domain interfaces, 1 computation engine

### Technical Constraints & Dependencies

- **Language/Runtime:** C# / .NET 8
- **Architecture pattern:** N-Tier with CQRS command/query separation; Small-Controller discipline
- **API documentation:** Swagger/OpenAPI (`Swashbuckle.AspNetCore`); all 40 enum types enumerated in schema
- **Auth:** JWT Bearer, RS256; `AddJwtBearer` middleware; policy-based authorization
- **Timestamps:** `DateTimeOffset` on all fields and DB columns — no `DateTime` permitted anywhere
- **Rate limiting:** Built-in .NET 8 `RateLimiter` middleware; 2 policies (standard 300 req/min, bulk 20 req/min) keyed by `sub` claim
- **API versioning:** URL path prefix `/api/v1/`; `Asp.Versioning` package
- **Error handling:** RFC 7807 Problem Details (`application/problem+json`) with distinct `type` URIs per error category
- **Computation:** Stateless — no computed results persisted (ADR-001). `ComputationResult` constructor enforces all 8 invariants.
- **Database:** TBD in Step 4 (SQL Server `datetimeoffset` or PostgreSQL `timestamptz`)

### Cross-Cutting Concerns Identified

1. **DateTimeOffset / Asia/Manila timezone** — All timestamp storage, calendar-day boundaries, midnight/10pm/6am split points, and "previous calendar day" comparisons use Manila local time. Every layer affected.
2. **JWT authentication & role-scoped RBAC** — Every endpoint requires authentication; every data-access operation is scoped by `sub` or `role` claim server-side.
3. **Stateless computation (ADR-001)** — No persistence of `ComputationResult`. Computation is always a full re-derivation. Affects caching strategy and report endpoint design (bulk-fetch, not cached segments).
4. **Idempotency** — Log submission and OT commit endpoints require `Idempotency-Key`; 5-minute deduplication window.
5. **Immutable audit logging** — OT commit and feature flag changes write to append-only audit records within the same DB transaction.
6. **Computation engine testability** — All 5 domain interfaces (`IHolidayCalendar`, `IClockProvider`, `IFeatureFlagProvider`, `ILogClaimTracker`, `IHolidayApprovalRepository`) must be injectable; computation layer must have zero infrastructure dependencies.
7. **RFC 7807 error contract** — All errors formatted as Problem Details with distinct `type` URIs; middleware-level enforcement.
8. **Rate limiting** — Applied after JWT validation; two policies distinguished by endpoint group; response headers on 429.

## Starter Template Evaluation

### Primary Technology Domain

API Backend (.NET 8) — internal REST API with CQRS command/query separation and Small-Controller discipline. No frontend, no real-time features, no multi-tenancy.

### Starter Options Considered

| Option | Notes |
|---|---|
| Jason Taylor Clean Architecture (`ca-sln`) | Well-maintained; API-only via `--clientFramework None`; now tracking .NET 10; pre-commits to MediatR |
| Ardalis Clean Architecture | Spec pattern and Result type are opinionated additions not needed here |
| **Vanilla `dotnet new webapi --use-controllers`** | **Selected** — PRD pre-specifies architecture; computation engine isolation is unique |

### Selected Approach: Vanilla dotnet new webapi

**Rationale:**
The PRD fully specifies the target architecture (N-Tier, CQRS, Small-Controller, 5 domain interfaces, stateless computation engine). Community templates make infrastructure choices (MediatR, Specification pattern, Result wrappers) that belong in Step 4 as explicit decisions, not silent defaults from a scaffold. Starting vanilla ensures every dependency is intentional.

**Initialization Command:**

```bash
dotnet new webapi --use-controllers --name ph-payroll-time-api
```

**Architectural Decisions Provided by Template:**

- **Runtime:** .NET 8 web host with `WebApplication.CreateBuilder`
- **Controller scaffold:** `[ApiController]` base + `ControllerBase` in `Controllers/`
- **OpenAPI:** `Swashbuckle.AspNetCore` package included; Swagger UI on `/swagger`
- **Nullable:** `<Nullable>enable</Nullable>` enabled by default
- **JSON:** `System.Text.Json` with default options (will override to `JsonStringEnumConverter`)
- **HTTP model:** Standard request/response; no minimal API endpoints
- **`launchSettings.json`:** Dev HTTPS profile with `applicationUrl` for localhost

**What the template does NOT provide (all decided in Step 4):**
- Layer/project structure (Domain, Application, Infrastructure)
- ORM / database driver
- CQRS dispatch mechanism (raw handlers vs MediatR)
- Authentication middleware
- Rate limiting configuration
- API versioning
- RFC 7807 error middleware

**Note:** Project initialization is the first implementation story.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- Database: PostgreSQL with EF Core 8 — all entity timestamps use `timestamptz`
- CQRS dispatch: Raw DI handlers — no mediator library dependency
- Auth: JWT RS256 via built-in `AddJwtBearer` — `alg: none` rejected at middleware

**Important Decisions (Shape Architecture):**
- EF Core global `DateTimeOffset` → `timestamptz` convention in `OnModelCreating`
- Serilog structured logging — replaces default Microsoft provider
- `IMemoryCache` for idempotency key deduplication (5-minute window, in-process)

**Deferred Decisions (Post-MVP):**
- Distributed cache (Redis) — only if horizontal scaling is ever needed
- CI/CD pipeline — not required for portfolio demo
- Cloud deployment target — local + Docker sufficient for portfolio

---

### Data Architecture

**Database:** PostgreSQL
- Provider: `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.8`
- EF Core: `Microsoft.EntityFrameworkCore 8.0.25`
- Dev: Docker (`postgres:16-alpine`) or local PostgreSQL install
- Migration tool: `dotnet-ef` CLI (`Microsoft.EntityFrameworkCore.Design 8.0.25`)

**EF Core global timestamp convention (applied in `OnModelCreating`):**
```csharp
builder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
builder.Properties<DateTimeOffset?>().HaveColumnType("timestamptz");
```
`DateTime` is prohibited throughout — nullable reference types + `DateTimeOffset` enforced globally.

**Migration strategy:** Code First. Each schema change = one named migration.
`dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project Api`

**Caching:** `IMemoryCache` (built-in) for idempotency key deduplication only.
No caching of computation results — stateless recomputation is ADR-001.
**Known trade-off:** `IMemoryCache` is process-local. A second API instance would have a separate cache and could process a duplicate request within the 5-minute window. Acceptable for single-instance portfolio demo; Redis would be required for horizontal scaling.

---

### CQRS Dispatch

**Pattern:** Raw DI handlers — no mediator library.

Define two generic handler interfaces in the `Application` layer:
```csharp
public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
```

Controllers inject `ICommandHandler<T>` or `IQueryHandler<T, R>` directly.
All business logic lives in handler implementations — controllers contain only
binding and response shaping (Small-Controller discipline).

**Rationale:** The computation engine enforces cross-cutting correctness through
domain invariants, not pipeline behaviors. A mediator abstraction adds indirection
without benefit. Raw handlers are faster, simpler, and demonstrate the pattern
without framework dependency.

---

### Authentication & Security

**Mechanism:** JWT Bearer, RS256.
- Package: `Microsoft.AspNetCore.Authentication.JwtBearer` (built-in .NET 8 SDK)
- Middleware: `AddJwtBearer` with `TokenValidationParameters` validating `iss`, `aud`, `exp`
- `alg: none` rejected: `ValidAlgorithms = ["RS256"]` in `TokenValidationParameters`
- Authorization: ASP.NET Core policy-based; roles from `role` claim
- Dev token issuer: built-in endpoint (`POST /api/v1/auth/token`) — non-production only

**Rate limiting:** Built-in `Microsoft.AspNetCore.RateLimiting` middleware.
- Standard policy: 300 req/min per `sub` claim (fixed window)
- Bulk policy: 20 req/min per `sub` claim (fixed window) — applied to batch + report endpoints
- Rate limit key: `sub` claim (not IP); keyed via `GetUserIdFromContext` resolver

---

### API & Communication Patterns

**REST:** All routes under `/api/v1/`. Controller-based (not minimal APIs).
**Versioning:** `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer`
**Documentation:** `Swashbuckle.AspNetCore` — all 40 enum types as strings via `JsonStringEnumConverter`
**Error handling:** RFC 7807 `ProblemDetails` middleware (`AddProblemDetails()` + custom exception handler)
**Idempotency:** `IMemoryCache` keyed on `SHA256(endpoint + Idempotency-Key header)`; 5-minute sliding TTL

---

### Infrastructure & Deployment

**Local development:** `dotnet run` or IDE; PostgreSQL via Docker or local install
**Portfolio demo:** `docker-compose.yml` with two services — `postgres:16-alpine` + API image
**Logging:** Serilog structured logging
- Packages: `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- Replaces default `Microsoft.Extensions.Logging` host provider
- Console sink: structured JSON (machine-readable) in production config
- Rolling file sink: daily rotation for local dev

**Environment config:** `appsettings.json` + `appsettings.{Environment}.json` + environment variables
(connection string, JWT signing key path, feature flag defaults)

---

### Testing Stack

| Layer | Tool | Purpose |
|---|---|---|
| Unit tests | xUnit + NSubstitute | Computation engine, domain logic, handler logic |
| Integration tests | xUnit + `WebApplicationFactory` | All 8 user journey scenarios; full HTTP stack |
| Mocking | NSubstitute | Substitute implementations of all 5 domain interfaces |
| DB for integration | `Testcontainers.PostgreSql` (optional) | Real PostgreSQL in integration tests via Docker |

**NFR-T4 compliance:** Computation engine unit tests inject NSubstitute stubs for
`IHolidayCalendar`, `IClockProvider`, `IFeatureFlagProvider`, `ILogClaimTracker`,
`IHolidayApprovalRepository` — zero infrastructure dependencies in the test assembly.

---

### Decision Impact Analysis

**Implementation sequence (dependency order):**
1. Domain layer — entities, value objects, 40-type enum, 5 interfaces, `ComputationResult`
2. Application layer — command/query handler interfaces + implementations
3. Infrastructure layer — EF Core `DbContext`, Npgsql setup, `timestamptz` convention, interface implementations
4. API layer — controllers, JWT middleware, rate limiting, versioning, Swagger, RFC 7807

**Cross-component dependencies:**
- Computation engine depends on all 5 domain interfaces → these are implemented in Infrastructure
- Controllers depend on handler interfaces → handlers implemented in Application
- `DateTimeOffset`/`timestamptz` convention flows through Domain → Infrastructure → DB schema
- JWT `sub` claim is the primary key for rate limiting, audit records, and scope enforcement

## Implementation Patterns & Consistency Rules

### Critical Conflict Points Identified

9 areas where AI agents would make different, incompatible choices without explicit rules:
database naming (snake_case vs PascalCase), handler folder layout, API response shape,
HTTP status code selection, EF Core tracking behaviour, exception-to-RFC7807 mapping,
test method naming, namespace root, and enum serialization.

---

### Naming Patterns

#### C# Code Naming

| Construct | Convention | Example |
|---|---|---|
| Classes, records, structs | `PascalCase` | `ComputationResult`, `TimeSegment` |
| Interfaces | `IPascalCase` | `IHolidayCalendar`, `ICommandHandler<T>` |
| Methods | `PascalCase` | `HandleAsync`, `ComputeSegments` |
| Properties | `PascalCase` | `ScheduleStart`, `EmployeeId` |
| Private fields | `_camelCase` | `_dbContext`, `_clockProvider` |
| Local variables / parameters | `camelCase` | `command`, `scheduleId`, `ct` |
| Constants | `PascalCase` | `MaxBatchSize`, `IdempotencyWindowMinutes` |
| Enums (type names) | `PascalCase` | `TimeSegmentClassification`, `HolidayType` |
| Enum members | `SCREAMING_SNAKE_CASE` | `NORMAL_OT`, `REGULAR_HOLIDAY_PAID_HOURS` |
| Namespaces | `PascalCase` segments | `PhPayrollTimeApi.Domain.Entities` |

#### Namespace Root

`PhPayrollTimeApi` — applied as the root for all four projects:
- `PhPayrollTimeApi.Domain`
- `PhPayrollTimeApi.Application`
- `PhPayrollTimeApi.Infrastructure`
- `PhPayrollTimeApi.Api`

#### Database Naming (PostgreSQL snake_case convention)

All EF Core entity configurations use explicit `.ToTable()` and `[Column]` to map
PascalCase C# names to PostgreSQL snake_case names.

| C# entity / property | DB table / column |
|---|---|
| `Employee` | `employees` |
| `WorkSchedulePattern` | `work_schedule_patterns` |
| `ShiftSchedule` | `shift_schedules` |
| `TimeLog` | `time_logs` |
| `HolidayCalendarEntry` | `holiday_calendar_entries` |
| `OtApproval` | `ot_approvals` |
| `AuditRecord` | `audit_records` |
| Property `EmployeeId` | column `employee_id` |
| Property `EffectiveDate` | column `effective_date` |
| Property `IsActive` | column `is_active` |

**EF Core naming convention:** All table names = entity type name converted to `snake_case`, pluralised.
All column names = property name converted to `snake_case`. Applied consistently via `IEntityTypeConfiguration<T>`.

#### Command / Query / Handler Naming

| Type | Pattern | Example |
|---|---|---|
| Command | `{Verb}{Noun}Command` | `CreateEmployeeCommand`, `SubmitTimeLogCommand` |
| Command handler | `{Verb}{Noun}CommandHandler` | `CreateEmployeeCommandHandler` |
| Query | `Get{Noun}Query` | `GetComputationQuery`, `GetAttendanceReportQuery` |
| Query handler | `Get{Noun}QueryHandler` | `GetComputationQueryHandler` |
| DTO (response) | `{Noun}Dto` or `{Noun}Response` | `TimeSegmentDto`, `ComputationResultResponse` |
| DTO (request body) | `{Verb}{Noun}Request` | `CreateEmployeeRequest`, `SubmitTimeLogRequest` |

#### API Route Naming

- Resources: **plural nouns** (`/employees`, `/schedules`, `/logs`, `/holidays`)
- Sub-resources: `/employees/{id}/schedules`, `/employees/{id}/logs`
- Approval queues: `/approvals/holiday-schedule`, `/approvals/rest-day-schedule`, `/approvals/overtime`
- All routes lowercase kebab-case (`/work-schedule-patterns`, `/rest-day-schedule`)
- Route parameters: `{id}`, `{employeeId}`, `{date}` — camelCase in C#, mapped from route

---

### Structure Patterns

#### Solution Layout

```
ph-payroll-time-api.sln
├── src/
│   ├── PhPayrollTimeApi.Domain/
│   ├── PhPayrollTimeApi.Application/
│   ├── PhPayrollTimeApi.Infrastructure/
│   └── PhPayrollTimeApi.Api/
└── tests/
    ├── PhPayrollTimeApi.Domain.Tests/
    ├── PhPayrollTimeApi.Application.Tests/
    └── PhPayrollTimeApi.Integration.Tests/
```

#### Domain Project Structure

```
Domain/
├── Entities/           # Employee, ShiftSchedule, TimeLog, WorkSchedulePattern, HolidayCalendarEntry
├── ValueObjects/       # TimeSegment, ComputationResult, BreakWindow
├── Enums/              # TimeSegmentClassification (40 types), HolidayType, ApprovalStatus
├── Interfaces/         # IHolidayCalendar, IClockProvider, IFeatureFlagProvider,
│                       #   ILogClaimTracker, IHolidayApprovalRepository
├── Exceptions/         # Domain-specific exceptions
└── Services/           # ComputationEngine (pure domain logic, no infrastructure deps)
```

#### Application Project Structure

```
Application/
├── Abstractions/       # ICommandHandler<T>, IQueryHandler<T,R>
├── Commands/
│   ├── CreateEmployee/
│   │   ├── CreateEmployeeCommand.cs
│   │   └── CreateEmployeeCommandHandler.cs
│   └── ... (one folder per command)
├── Queries/
│   ├── GetComputation/
│   │   ├── GetComputationQuery.cs
│   │   └── GetComputationQueryHandler.cs
│   └── ... (one folder per query)
└── DTOs/               # Shared request/response data transfer objects
```

**Rule:** Each command or query lives in its own subfolder containing exactly the
command/query class and its handler. No sharing folders between different operations.

#### Infrastructure Project Structure

```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/     # IEntityTypeConfiguration<T> per entity
│   └── Migrations/
├── Services/               # Implementations of domain interfaces
│   ├── EfHolidayCalendar.cs
│   ├── SystemClockProvider.cs
│   ├── FeatureFlagProvider.cs
│   ├── InMemoryLogClaimTracker.cs
│   └── EfHolidayApprovalRepository.cs
└── Extensions/             # IServiceCollection extension for DI registration
```

#### API Project Structure

```
Api/
├── Controllers/        # One controller per resource group
├── Middleware/         # IdempotencyMiddleware, ExceptionHandlerMiddleware
├── Extensions/         # IServiceCollection extensions (auth, rate limiting, swagger)
├── Filters/            # Action filters if needed
└── Program.cs          # Composition root
```

---

### Format Patterns

#### API Response Format

- **Success (single resource):** Return the resource DTO directly — no wrapper object.
- **Success (collection):** Return array directly — no envelope at MVP.
- **Created resource:** `201 Created` with `Location` header; body contains created resource DTO.
- **Delete:** `204 No Content` — no body.
- **Update:** `200 OK` with updated resource DTO.
- **Error:** RFC 7807 `application/problem+json` always.

#### HTTP Status Code Rules

| Scenario | Status Code |
|---|---|
| Successful GET / PUT | 200 OK |
| Successful POST (create) | 201 Created |
| Successful DELETE | 204 No Content |
| Request body validation failure | 400 Bad Request |
| Missing/invalid JWT | 401 Unauthorized |
| Valid JWT, insufficient role/scope | 403 Forbidden |
| Entity not found | 404 Not Found |
| Schedule overlap / stale approval conflict | 409 Conflict |
| Computation invariant violation | 422 Unprocessable Entity |
| Rate limit exceeded | 429 Too Many Requests |

#### JSON Serialization Rules

- Field names: **camelCase** (System.Text.Json default)
- Enum values: **SCREAMING_SNAKE_CASE strings** via `JsonStringEnumConverter` globally
- DateTimeOffset: ISO 8601 with UTC offset (`2026-05-01T08:00:00+08:00`)
- DateOnly: `YYYY-MM-DD` (`2026-05-01`)
- Null fields: **omitted** (`DefaultIgnoreCondition = WhenWritingNull`)
- No custom `JsonPropertyName` annotations unless wire value differs from C# property name

#### RFC 7807 Problem Details Format

```json
{
  "type": "https://ph-payroll-time-api/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Employee with ID 'abc123' was not found.",
  "instance": "/api/v1/employees/abc123"
}
```

`type` URI format: `https://ph-payroll-time-api/errors/{slug}` — slugs defined in a central
`ProblemTypes` static class, never hardcoded in handlers.

---

### Process Patterns

#### Exception Hierarchy and RFC 7807 Mapping

| Exception type | HTTP Status | `type` slug |
|---|---|---|
| `EntityNotFoundException` | 404 | `not-found` |
| `ValidationException` | 400 | `validation` |
| `ScheduleOverlapException` | 409 | `conflict/overlapping-schedule` |
| `StaleApprovalException` | 409 | `conflict/stale-approval` |
| `ComputationInvariantException` | 422 | `computation-invariant` |
| `UnauthorizedAccessException` | 403 | `forbidden` |
| Unhandled exception | 500 | `internal-error` |

Domain exceptions propagate from handlers; global `ExceptionHandlerMiddleware` catches
and maps them. Never catch domain exceptions inside handlers.

#### EF Core Query Patterns

- **Read queries:** always `.AsNoTracking()` — no exceptions.
- **Write operations:** load entity with tracking, modify, save.
- **No lazy loading:** navigation properties loaded explicitly with `.Include()`.
- **No raw SQL string concatenation:** only parameterized `FromSqlRaw` or LINQ.
- **Bulk-fetch for reports:** all data loaded in set-based queries before computation — never query inside a loop.

#### Async / CancellationToken Pattern

All `HandleAsync` methods and EF Core operations accept and pass `CancellationToken ct`.
No `Task.Run` wrappers around synchronous domain logic.

#### Idempotency Key Pattern

Handled entirely by `IdempotencyMiddleware` — not inside command handlers.
Cache key: `SHA256(httpMethod + path + Idempotency-Key header value)`. 5-minute sliding TTL in `IMemoryCache`.

#### Test Method Naming

`{MethodUnderTest}_When{Scenario}_Should{ExpectedResult}`

Examples:
- `Compute_WhenShiftCrossesIntoRegularHoliday_EmitsRegularHolidayOtSegment`
- `Compute_WhenInvariantViolated_CollectsAllViolationsBeforeThrowing`
- `PairLogs_WhenNoPriorDayOut_ReturnsAbsentAfterScheduleEnd`

---

### Enforcement Guidelines

**All AI Agents MUST:**

1. Use `SCREAMING_SNAKE_CASE` for all `TimeSegmentClassification` enum members — wire values, never renamed
2. Map C# names to PostgreSQL `snake_case` via explicit EF Core `IEntityTypeConfiguration<T>` — never rely on EF defaults
3. Use `DateTimeOffset` everywhere — `DateTime` is prohibited
4. Evaluate calendar-day boundaries via `IClockProvider` — never call `DateTimeOffset.UtcNow` directly in domain/application layers
5. Inject all 5 domain interfaces — never instantiate infrastructure services directly in domain or application
6. Return RFC 7807 Problem Details for all errors — no plain string error bodies
7. Apply `.AsNoTracking()` on all read queries
8. Place each command + handler in its own subfolder under `Application/Commands/` or `Application/Queries/`
9. Accept `CancellationToken ct` in every async method and pass it through

**Anti-Patterns (never do these):**

- `DateTime.UtcNow` in domain/application layers → use `IClockProvider.UtcNow`
- Persisting `ComputationResult` to DB → stateless recomputation, ADR-001
- `return Ok(new { error = "message" })` → throw typed domain exception, middleware maps it
- EF Core read query without `.AsNoTracking()` → always explicit on GET handlers
- Hardcoded holiday data → always via `IHolidayCalendar`
- `throw new Exception(...)` → always use typed domain exception class

## Project Structure & Boundaries

### Requirements to Structure Mapping

| FR Area | Domain | Application | Infrastructure | Api |
|---|---|---|---|---|
| Employee & Schedule Mgmt (FR1–FR8) | `Employee`, `ShiftSchedule`, `WorkSchedulePattern` entities | 7 commands + 2 queries | EF configs for 3 entities | `EmployeesController`, `SchedulesController`, `WorkSchedulePatternsController` |
| Time Log Recording (FR9–FR13) | `TimeLog` entity | `SubmitTimeLog` command + `ListTimeLogs` query | EF config, idempotency middleware | `TimeLogsController` |
| Computation Engine (FR14–FR25) | `ComputationEngine`, `TimeSegment`, `ComputationResult`, 40-type enum, 5 interfaces | `GetComputation` query | 5 interface implementations | `ComputationController` |
| Approval Workflow (FR26–FR38) | `HolidayScheduleApproval`, `RestDayScheduleApproval`, `OtApproval`, `StagedOtAction`, `AuditRecord` | 5 commands + 3 queue queries | EF configs, audit write in transaction | `ApprovalsController` |
| Holiday Calendar (FR39–FR42) | `HolidayCalendarEntry` entity | 3 commands + `ListHolidays` query | EF config | `HolidaysController` |
| Attendance Reporting (FR43–FR46) | — | `GetAttendanceReport` query | Bulk-fetch strategy in handler | `ReportsController` |
| Feature Flags (FR47–FR49) | `IFeatureFlagProvider` interface | `GetFeatureFlags` query + `ToggleFeatureFlag` command | `DbFeatureFlagProvider` + audit | `FeatureFlagsController` |
| API Platform & Security (FR50–FR56) | — | — | — | JWT middleware, rate limiting, Swagger, RFC 7807, `AuthController` |

---

### Complete Project Directory Structure

```
ph-payroll-time-api/
├── .gitignore
├── .dockerignore
├── docker-compose.yml                    # postgres:16-alpine + api services
├── docker-compose.override.yml           # dev overrides (ports, env vars)
├── ph-payroll-time-api.sln
├── README.md
│
├── src/
│   │
│   ├── PhPayrollTimeApi.Domain/
│   │   ├── PhPayrollTimeApi.Domain.csproj   # no NuGet deps — pure C#
│   │   ├── Entities/
│   │   │   ├── Employee.cs
│   │   │   ├── ShiftSchedule.cs             # cross-date supported; BreakWindow owned
│   │   │   ├── WorkSchedulePattern.cs       # RestDays: ICollection<DayOfWeek>
│   │   │   ├── TimeLog.cs
│   │   │   ├── HolidayCalendarEntry.cs
│   │   │   ├── HolidayScheduleApproval.cs
│   │   │   ├── RestDayScheduleApproval.cs
│   │   │   ├── OtApproval.cs
│   │   │   ├── StagedOtAction.cs
│   │   │   └── AuditRecord.cs               # append-only
│   │   ├── ValueObjects/
│   │   │   ├── TimeSegment.cs               # start, end, durationMinutes, classification
│   │   │   ├── ComputationResult.cs         # constructor enforces all 8 invariants
│   │   │   └── BreakWindow.cs
│   │   ├── Enums/
│   │   │   ├── TimeSegmentClassification.cs # 40 SCREAMING_SNAKE_CASE members
│   │   │   ├── HolidayType.cs               # REGULAR | SPECIAL_NON_WORKING | SPECIAL_WORKING | NONE
│   │   │   ├── ApprovalStatus.cs            # Pending | Approved | Rejected
│   │   │   └── UserRole.cs                  # Employee | Manager | HrAdmin
│   │   ├── Interfaces/
│   │   │   ├── IHolidayCalendar.cs
│   │   │   ├── IClockProvider.cs
│   │   │   ├── IFeatureFlagProvider.cs
│   │   │   ├── ILogClaimTracker.cs
│   │   │   └── IHolidayApprovalRepository.cs
│   │   ├── Exceptions/
│   │   │   ├── EntityNotFoundException.cs
│   │   │   ├── DomainValidationException.cs
│   │   │   ├── ScheduleOverlapException.cs
│   │   │   ├── StaleApprovalException.cs
│   │   │   └── ComputationInvariantException.cs  # carries List<string> violations
│   │   └── Services/
│   │       ├── ComputationEngine.cs         # pure domain logic; all 5 interfaces injected
│   │       └── LogPairingService.cs         # implements full pairing algorithm
│   │
│   ├── PhPayrollTimeApi.Application/
│   │   ├── PhPayrollTimeApi.Application.csproj  # ref: Domain only
│   │   ├── Abstractions/
│   │   │   ├── ICommandHandler.cs           # Task HandleAsync(TCommand, CancellationToken)
│   │   │   └── IQueryHandler.cs             # Task<TResult> HandleAsync(TQuery, CancellationToken)
│   │   ├── Commands/
│   │   │   ├── CreateEmployee/
│   │   │   │   ├── CreateEmployeeCommand.cs
│   │   │   │   └── CreateEmployeeCommandHandler.cs
│   │   │   ├── UpdateEmployee/
│   │   │   ├── CreateSchedule/
│   │   │   ├── UpdateSchedule/
│   │   │   ├── DeleteSchedule/
│   │   │   ├── AssignWorkSchedulePattern/
│   │   │   ├── UpdateWorkSchedulePattern/
│   │   │   ├── SubmitTimeLog/
│   │   │   ├── BulkApproveHolidaySchedule/
│   │   │   ├── BulkApproveRestDaySchedule/
│   │   │   ├── StageOtAction/
│   │   │   ├── RemoveStagedOtAction/
│   │   │   ├── CommitOtApprovals/
│   │   │   ├── CreateHoliday/
│   │   │   ├── UpdateHoliday/
│   │   │   ├── DeleteHoliday/
│   │   │   └── ToggleFeatureFlag/
│   │   ├── Queries/
│   │   │   ├── GetEmployee/
│   │   │   ├── ListSchedules/
│   │   │   ├── ListTimeLogs/
│   │   │   ├── GetComputation/
│   │   │   │   ├── GetComputationQuery.cs
│   │   │   │   └── GetComputationQueryHandler.cs  # calls ComputationEngine
│   │   │   ├── GetHolidayScheduleApprovalQueue/
│   │   │   ├── GetRestDayScheduleApprovalQueue/
│   │   │   ├── GetOtApprovalQueue/
│   │   │   ├── ListHolidays/
│   │   │   ├── GetAttendanceReport/              # bulk-fetch strategy
│   │   │   └── GetFeatureFlags/
│   │   └── DTOs/
│   │       ├── Requests/                         # {Verb}{Noun}Request.cs per operation
│   │       └── Responses/                        # {Noun}Response.cs / {Noun}Dto.cs
│   │
│   ├── PhPayrollTimeApi.Infrastructure/
│   │   ├── PhPayrollTimeApi.Infrastructure.csproj  # ref: Domain + Application
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs               # global timestamptz convention in OnModelCreating
│   │   │   ├── Configurations/
│   │   │   │   ├── EmployeeConfiguration.cs
│   │   │   │   ├── ShiftScheduleConfiguration.cs
│   │   │   │   ├── WorkSchedulePatternConfiguration.cs
│   │   │   │   ├── TimeLogConfiguration.cs
│   │   │   │   ├── HolidayCalendarEntryConfiguration.cs
│   │   │   │   ├── HolidayScheduleApprovalConfiguration.cs
│   │   │   │   ├── RestDayScheduleApprovalConfiguration.cs
│   │   │   │   ├── OtApprovalConfiguration.cs
│   │   │   │   ├── StagedOtActionConfiguration.cs
│   │   │   │   └── AuditRecordConfiguration.cs
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── EfHolidayCalendar.cs          # implements IHolidayCalendar via DbContext
│   │   │   ├── SystemClockProvider.cs        # implements IClockProvider → DateTimeOffset.UtcNow
│   │   │   ├── DbFeatureFlagProvider.cs      # implements IFeatureFlagProvider via DB (Epic 8)
│   │   │   ├── HardcodedFeatureFlagProvider.cs  # stub for Epics 1–7: returns all flags enabled
│   │   │   ├── InMemoryLogClaimTracker.cs    # implements ILogClaimTracker — tracks which TimeLog
│   │   │   │                                 #   IDs were claimed by a prior schedule entry during
│   │   │   │                                 #   a single Compute() call; prevents double-assignment
│   │   │   │                                 #   in the log pairing algorithm (scoped per request)
│   │   │   └── EfHolidayApprovalRepository.cs  # used by ComputationEngine to query HolidaySchedule
│   │   │                                        #   Approval / RestDayScheduleApproval records only;
│   │   │                                        #   OT approval state managed in Application handlers
│   │   └── Extensions/
│   │       └── InfrastructureServiceExtensions.cs  # registers DbContext, all 5 interface impls
│   │
│   └── PhPayrollTimeApi.Api/
│       ├── PhPayrollTimeApi.Api.csproj         # ref: Application + Infrastructure (DI only)
│       ├── Program.cs                          # composition root; middleware pipeline order
│       ├── Controllers/
│       │   ├── AuthController.cs               # POST /api/v1/auth/token — dev/test only
│       │   ├── EmployeesController.cs          # POST, GET /{id}, PUT /{id}
│       │   ├── WorkSchedulePatternsController.cs
│       │   ├── SchedulesController.cs
│       │   ├── TimeLogsController.cs
│       │   ├── ComputationController.cs        # GET /schedules/{id}/computation
│       │   ├── HolidaysController.cs
│       │   ├── ApprovalsController.cs          # holiday-schedule, rest-day-schedule, overtime
│       │   ├── ReportsController.cs            # GET /reports/attendance
│       │   └── FeatureFlagsController.cs       # GET + PUT /config/feature-flags
│       ├── Middleware/
│       │   ├── IdempotencyMiddleware.cs        # SHA256 key → IMemoryCache 5-min TTL
│       │   └── ExceptionHandlerMiddleware.cs   # domain exception → RFC 7807 mapping
│       ├── Extensions/
│       │   ├── AuthExtensions.cs               # AddJwtBearer, ValidAlgorithms=["RS256"]
│       │   ├── RateLimitingExtensions.cs       # standard (300/min) + bulk (20/min) policies
│       │   ├── SwaggerExtensions.cs            # JsonStringEnumConverter, ProblemDetails schemas
│       │   ├── ApiVersioningExtensions.cs      # Asp.Versioning, /api/v1/ prefix
│       │   └── ApplicationServiceExtensions.cs # all ICommandHandler + IQueryHandler DI registrations
│       ├── Constants/
│       │   └── ProblemTypes.cs                 # all "https://ph-payroll-time-api/errors/…" URIs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Dockerfile
│
└── tests/
    ├── PhPayrollTimeApi.Domain.Tests/
    │   ├── PhPayrollTimeApi.Domain.Tests.csproj
    │   ├── ComputationEngine/
    │   │   ├── RegularDayTests.cs              # NFR-T1: all 6 regular-day types ×2
    │   │   ├── RegularHolidayTests.cs          # 6 types ×2
    │   │   ├── SpecialHolidayTests.cs          # 6 types ×2
    │   │   ├── RestDayTests.cs                 # 6 types ×2
    │   │   ├── RestDayRegularHolidayTests.cs   # 6 types ×2
    │   │   ├── RestDaySpecialHolidayTests.cs   # 6 types ×2
    │   │   ├── TaggedTerminalTests.cs          # 4 types ×2
    │   │   ├── InvariantEnforcementTests.cs    # NFR-T2: all 8 invariants
    │   │   └── NightDifferentialBoundaryTests.cs
    │   └── LogPairing/
    │       └── LogPairingAlgorithmTests.cs
    │
    ├── PhPayrollTimeApi.Application.Tests/
    │   ├── PhPayrollTimeApi.Application.Tests.csproj
    │   └── Handlers/
    │       ├── Commands/                        # handler unit tests with NSubstitute stubs
    │       └── Queries/
    │
    └── PhPayrollTimeApi.Integration.Tests/
        ├── PhPayrollTimeApi.Integration.Tests.csproj
        ├── Fixtures/
        │   └── ApiTestFixture.cs               # WebApplicationFactory; real DB or Testcontainers
        └── Journeys/                           # NFR-T3: all 8 journeys as integration tests
            ├── Journey1_AnaEarlyOtCrossMidnightHolidayTests.cs
            ├── Journey2_MarcoHolidayNightShiftTests.cs
            ├── Journey3_CarlosHolidayApprovalTests.cs
            ├── Journey4_CarlosOtAdjustmentCascadeTests.cs
            ├── Journey5_CarlosMissedPunchCorrectionTests.cs
            ├── Journey6_MariaHrOnboardingFeatureFlagTests.cs
            ├── Journey7_DevTeamSwaggerDiscoveryTests.cs
            └── Journey8_AttendanceReportRoleScopedTests.cs
```

---

### Architectural Boundaries

#### Layer Dependency Rules

```
Domain  ←  Application  ←  Infrastructure
                ↑                 ↑
              Api  ←————————————(DI registration only)
```

- **Domain:** zero NuGet dependencies; no EF Core, no HTTP references
- **Application:** references Domain only; no DbContext, no HttpContext
- **Infrastructure:** references Domain + Application; owns DbContext and interface implementations
- **Api:** references Application (for handler interfaces) + Infrastructure (DI composition root only)
- **Tests:** each test project references only the layer it's testing

#### Data Flow — Computation Request

**Schedule resolution strategy:** `GetComputationQueryHandler` (Application layer) loads the `ShiftSchedule`, its associated `WorkSchedulePattern`, and all relevant `TimeLog` records from the DB via `AppDbContext` (AsNoTracking), then passes them as value objects to `ComputationEngine.Compute()`. The domain engine never queries the DB directly — it receives all schedule context as method arguments. This is the ADR-001-consistent pattern: Application layer assembles input, Domain layer computes output.

```
GET /api/v1/schedules/{id}/computation
  → ComputationController
  → GetComputationQuery → IQueryHandler<GetComputationQuery, ComputationResultResponse>
  → GetComputationQueryHandler (Application)
      loads ShiftSchedule + WorkSchedulePattern + TimeLogs via DbContext (AsNoTracking)
      calls ComputationEngine.Compute(schedule, workPattern, logs, IHolidayCalendar,
            IClockProvider, IFeatureFlagProvider, ILogClaimTracker,
            IHolidayApprovalRepository)
  → ComputationEngine (Domain) — pure function, no infrastructure access
      resolves rest-day status from workPattern.RestDays
      builds boundary list → classifies segments → ComputationResult constructor
      ComputationResult constructor enforces all 8 invariants (throws if violated)
  → handler maps ComputationResult → ComputationResultResponse DTO
  → 200 OK with ComputationResultResponse
```

#### Data Flow — OT Commit (Write + Audit)

**ADR-001 + approval state:** `OtApproval` records store only the approval status and any manager-committed segment overrides (reduced duration, classification override, rejection flag). No pre-computed classification hours are stored in the database. All hours are derived on-demand by `ComputationEngine` using the stored approval records as input on each subsequent read. This preserves ADR-001 (stateless recomputation) throughout the approval lifecycle.

```
POST /api/v1/approvals/overtime/commit  (+Idempotency-Key header)
  → IdempotencyMiddleware checks IMemoryCache → pass-through or return cached
  → ApprovalsController
  → CommitOtApprovalsCommand → ICommandHandler<CommitOtApprovalsCommand>
  → CommitOtApprovalsCommandHandler (Application)
      loads staged actions + current computation state
      validates all staged actions against current state (pre-commit validation)
      if any conflict → throws StaleApprovalException (rolls back, returns 409)
      begin DB transaction:
        write OtApproval records (status + overrides only — no computed hours)
        write AuditRecord (immutable, same transaction)
        delete StagedOtAction records
      commit transaction
  → IdempotencyMiddleware caches 201 response (5-min TTL)
  → 201 Created
```

#### Cross-Cutting Concern Locations

| Concern | Location |
|---|---|
| JWT auth & RBAC | `Api/Extensions/AuthExtensions.cs` + `[Authorize(Policy = "...")]` on controllers |
| Rate limiting | `Api/Extensions/RateLimitingExtensions.cs` + `[EnableRateLimiting("...")]` on controller groups |
| Idempotency | `Api/Middleware/IdempotencyMiddleware.cs` (before controller routing) |
| RFC 7807 error mapping | `Api/Middleware/ExceptionHandlerMiddleware.cs` |
| `timestamptz` global convention | `Infrastructure/Persistence/AppDbContext.cs` `OnModelCreating` |
| Asia/Manila calendar day evaluation | `Domain/Services/ComputationEngine.cs` via `IClockProvider` |
| Audit logging | `Application/Commands/CommitOtApprovals/` + `Application/Commands/ToggleFeatureFlag/` — written in same DB transaction |

---

### Development Workflow

**Run locally:**
```bash
docker compose up -d postgres
dotnet ef database update --project src/PhPayrollTimeApi.Infrastructure \
                           --startup-project src/PhPayrollTimeApi.Api
dotnet run --project src/PhPayrollTimeApi.Api
# Swagger UI: https://localhost:{port}/swagger
```

**Full Docker demo:**
```bash
docker compose up --build
```

**Add a migration:**
```bash
dotnet ef migrations add <MigrationName> \
  --project src/PhPayrollTimeApi.Infrastructure \
  --startup-project src/PhPayrollTimeApi.Api
```

**Run tests:**
```bash
dotnet test tests/PhPayrollTimeApi.Domain.Tests           # pure unit — no Docker needed
dotnet test tests/PhPayrollTimeApi.Application.Tests      # handler tests with NSubstitute
dotnet test tests/PhPayrollTimeApi.Integration.Tests      # requires PostgreSQL
```

## Architecture Validation Results

### Coherence Validation

**Decision Compatibility:**
All technology versions are co-compatible within the .NET 8 LTS line:
`Microsoft.EntityFrameworkCore 8.0.25` + `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.8` +
`Microsoft.AspNetCore.Authentication.JwtBearer` (built-in) + `Asp.Versioning.Mvc` +
`Serilog.AspNetCore` — no cross-package version conflicts.

**Pattern Consistency:**
`SCREAMING_SNAKE_CASE` enum members + `JsonStringEnumConverter` produce wire values that
exactly match C# member names — no aliasing needed. `DateTimeOffset` global convention +
`IClockProvider` abstraction is consistently applied from domain through to DB column type.
Raw DI handlers align with Small-Controller discipline: zero framework ceremony.

**Structure Alignment:**
Layer dependency direction is enforced by project references. Domain has zero NuGet dependencies.
Application references Domain only. Infrastructure owns all EF Core and external dependencies.
Api is the sole composition root. The structure physically enforces the boundary rules.

---

### Requirements Coverage Validation

**Functional Requirements (56 FRs):** All covered — every FR area maps to specific entities,
handlers, and controllers in the project tree. No FR category lacks architectural support.

**Non-Functional Requirements (19 NFRs):** All covered:
- Performance: stateless computation (no result persistence); `AsNoTracking()` on reads; bulk-fetch on reports
- Security: `ValidAlgorithms = ["RS256"]`; policy-based auth from claims; rate limit key per `sub` claim
- Reliability: DB transaction for OT commit + audit write; idempotency middleware; NTP noted as deployment concern
- Testability: Domain project has zero infra deps; 5 interfaces injectable via NSubstitute; all 8 journey tests mapped

---

### Implementation Readiness — Gap Analysis

**Critical Gaps:** None.

**Important Gaps (resolved below — apply during first implementation story):**

**Gap 1 — `Program.cs` middleware pipeline order.**
Required order (NFR-S5 mandates sub-claim-keyed rate limiting — `UseAuthentication` MUST precede `UseRateLimiter`):

```csharp
// Program.cs middleware pipeline — MANDATORY ORDER
app.UseExceptionHandler();                     // 1. catches everything — must be first
app.UseHttpsRedirection();                     // 2. HTTPS enforcement
app.UseSerilogRequestLogging();                // 3. structured request logs
app.UseAuthentication();                       // 4. JWT validation — must precede rate limiter
app.UseAuthorization();                        // 5. policy enforcement
app.UseRateLimiter();                          // 6. rate limit keyed by sub claim (NFR-S5)
app.UseMiddleware<IdempotencyMiddleware>();     // 7. after auth (needs sub claim)
app.MapControllers();                          // 8. route to controllers
```

**Gap 2 — `WorkSchedulePattern.RestDays` (`ICollection<DayOfWeek>`) PostgreSQL storage.**
Use Npgsql native `integer[]` — no junction table:

```csharp
// WorkSchedulePatternConfiguration.cs
builder.Property(x => x.RestDays)
       .HasColumnType("integer[]")
       .HasConversion(
           v => v.Select(d => (int)d).ToArray(),
           v => v.Select(i => (DayOfWeek)i).ToList());
```

Same pattern applies to `WorkDays`.

**Gap 3 — 8 `ComputationResult` invariants (enumerated for NFR-T2 test authoring).**

The `ComputationResult` constructor enforces these 8 invariants and collects all violations before throwing `ComputationInvariantException`:

| # | Invariant | Rule |
|---|---|---|
| 1 | **Minute Conservation** | Every minute within the schedule window appears in exactly one segment — no unaccounted minutes |
| 2 | **No Overlap** | No two segments share any minute |
| 3 | **No Zero-Duration** | Every segment has duration > 0 minutes |
| 4 | **Classification Exclusivity** | Each segment carries exactly one `TimeSegmentClassification` value |
| 5 | **Regular Holiday Gate** | `REGULAR_HOLIDAY_*` classifications appear only when the shift start date is a regular holiday |
| 6 | **Special Holiday Gate** | `SPECIAL_*` classifications appear only when the shift start date is a special non-working holiday |
| 7 | **OT Bounds** | Overtime segments begin only after the employee's regular-hours threshold has been exhausted |
| 8 | **Schedule-Hours Bounds** | Total segment duration (after break deductions) equals the clocked interval duration |

**Gap 4 — Feature flag stub for Epics 1–7.**

`DbFeatureFlagProvider` is implemented in Epic 8. For all preceding epics, `HardcodedFeatureFlagProvider` (all flags return `true`/enabled) is registered in DI instead. When Epic 8 stories are complete, DI registration is switched to `DbFeatureFlagProvider`. This ensures integration tests for Epics 1–7 do not fail due to missing feature flag infrastructure.

**Gap 5 — EF Core migration strategy.**

All domain entity classes must be defined before the first migration is applied. The implementation policy: scaffold all entity classes in the Domain project as part of Epic 1's domain modeling stories, then apply a single baseline migration (`InitialSchema`). Subsequent epics add new columns or tables via additive named migrations only. No migration should destructively alter a table created by a prior epic.

**Gap 6 — Portfolio demo seed data.**

`AppDbContext` (or a dedicated `DataSeeder`) must seed realistic PH DOLE scenario data to support the portfolio demo. Seed data must include: at least one employee per role, at least one regular holiday + one special non-working holiday entry, sample shift schedules spanning midnight and the night differential window, and sample time logs for all 8 user journey scenarios. Seed data is applied in Epic 1 via a seeding extension method called from `Program.cs` in the `Development` environment only.

**Nice-to-Have Gaps (non-blocking, post-MVP):**
- `appsettings.json` key schema — define connection string key names at project init
- Testcontainers vs manual Docker for integration tests — story-level decision

---

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**
- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

---

### Architecture Readiness Assessment

**Overall Status: READY FOR IMPLEMENTATION**

All 16 checklist items confirmed. No critical gaps. Two important gaps (middleware order +
`RestDays` storage) resolved above — apply in the first implementation story.

**Confidence Level:** High

**Key Strengths:**
- Computation engine is architecturally isolated — pure domain logic, zero infra dependencies, testable from day one
- Stateless recomputation (ADR-001) eliminates an entire class of consistency bugs
- All 9 AI agent conflict points explicitly addressed with rules and examples
- 40-type classification taxonomy documented end-to-end: domain enum → DB → API wire values → test files
- Layer dependency rules physically enforced by project references, not just convention

**Areas for Future Enhancement:**
- Testcontainers integration for fully containerised CI test runs
- Health check endpoint with `holiday_calendar_last_updated_days` metric (PRD nice-to-have)
- Pagination on list endpoints (currently returns all within date range)

---

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented
- Use implementation patterns and naming conventions consistently across all components
- Respect layer boundaries — check project reference rules before adding any dependency
- Apply Gap 1 (middleware order) and Gap 2 (RestDays storage) in the first implementation story
- Refer to this document for all architectural decisions; refer to the PRD for all domain rules

**First Implementation Priority:**
```bash
dotnet new webapi --use-controllers --name ph-payroll-time-api
```
Then scaffold the 4-project solution structure (`Domain`, `Application`, `Infrastructure`, `Api`)
and 3 test projects before writing any domain logic.
