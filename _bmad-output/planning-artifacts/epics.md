---
stepsCompleted: [step-01-validate-prerequisites, step-02-design-epics, step-03-create-stories, step-04-final-validation]
inputDocuments: ['_bmad-output/planning-artifacts/prd.md', '_bmad-output/planning-artifacts/architecture.md']
---

# ph-payroll-time-api - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for ph-payroll-time-api, decomposing the requirements from the PRD and Architecture into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: Manager and HR Admin can create an employee profile
FR2: HR Admin can update an employee profile
FR3: Manager and HR Admin can create a shift schedule for an employee, including cross-date shifts and a break window
FR4: Manager and HR Admin can update or delete an existing shift schedule for an employee
FR5: The system prevents creation of overlapping schedules for the same employee on any shared date
FR6: Manager and HR Admin can assign a work schedule pattern to an employee with an effective date and optional expiry date
FR7: Manager and HR Admin can update a work schedule pattern's effective or expiry date
FR8: Employee can view their own schedules
FR9: Employee can submit a time log at the current server time only
FR10: Manager and HR Admin can submit a time log for any employee on any date
FR11: Manager and HR Admin can view all time logs for any employee within a date range
FR12: Employee can view their own time logs
FR13: The system deduplicates time log submissions sharing the same Idempotency-Key within the 5-minute deduplication window
FR14: The system classifies every minute of a schedule entry into exactly one of the 40 defined classification types, determined by schedule boundaries, time logs, holiday calendar, work schedule pattern, and approval state
FR15: The system deducts break time as the intersection of the shift's break window with the employee's clocked interval
FR16: The system splits any interval spanning the night differential window (10pm–6am) at the 10pm and 6am boundaries, applying the night differential classification variant to the inside portion
FR17: The system derives rest day status for an employee and date from the employee's active WorkSchedulePattern; rest day is not inferred from shift absence
FR18: The system governs holiday and rest day classification by the shift start date; segments that cross midnight inherit the rules applicable at shift start
FR19: The system pairs IN and OUT logs using the full log pairing algorithm: same-day or immediately prior calendar day, earliest valid IN from prior day, OUT not within or after the next schedule's time range, IN not within or before another schedule's time range
FR20: When no valid OUT exists and server time is before schedule end, the system returns IN_PROGRESS as the sole result for that schedule entry
FR21: When no valid OUT exists and server time is at or after schedule end, the system returns ABSENT
FR22: On a Regular Holiday date with no approved schedule and no time logs, the system returns REGULAR_HOLIDAY_REST (100% paid by Art. 94 entitlement)
FR23: On a Special Non-Working Holiday date with no approved schedule and no time logs, the system returns no result (no pay, no deduction per RA 9492)
FR24: When time logs are present regardless of approval status, the system applies statutory compensation rates and flags the computation result for HR review
FR25: The system rejects any computation result that violates any of the 8 defined invariants (Minute Conservation, No Overlap, No Zero-Duration, Classification Exclusivity, Regular Holiday Gate, Special Holiday Gate, OT Bounds, Schedule-Hours Bounds), collecting all violations before rejection
FR26: Manager can view pending Holiday Schedule Approval requests scoped to their direct reports
FR27: Manager can bulk approve or reject Holiday Schedule Approval requests for up to 100 employees per request
FR28: Manager can view pending Rest Day Schedule Approval requests scoped to their direct reports
FR29: Manager can bulk approve or reject Rest Day Schedule Approval requests for up to 100 employees per request
FR30: When a rest day date coincides with a non-working holiday, the system issues a single combined approval request covering both rest day schedule and holiday entitlement; both cannot be independently approved or rejected
FR31: Manager can view pending OT Approval requests scoped to their direct reports, including all staged segment classifications and durations
FR32: Manager can stage OT adjustment actions (reduce duration, override classification, reject) for individual employee segments before committing
FR33: Manager can remove a staged OT action before committing
FR34: The system automatically removes and reinstates OT segment classifications when a manager reduces OT duration past a classification boundary (midnight or 10pm night diff threshold)
FR35: Manager can atomically commit all staged OT actions for one or more employees in a single request; the system validates all staged actions against current computation state before committing and rejects the entire batch if any conflict is detected
FR36: Holiday Schedule Approval and Rest Day Schedule Approval are prerequisite gates for OT Approval on the same date
FR37: The system deduplicates OT commit requests sharing the same Idempotency-Key within the deduplication window
FR38: The system writes an immutable audit record of all staged actions, cascade effects, actor, and timestamp on each successful OT commit
FR39: All authenticated users can view PH holiday calendar entries filtered by date range
FR40: HR Admin can add, update, and remove holiday entries with a date, name, and HOLIDAY_TYPE (REGULAR, SPECIAL_NON_WORKING, SPECIAL_WORKING, NONE)
FR41: The system treats Special Working Day dates as ordinary working days with no pay premium
FR42: The system flags existing committed OT approvals as stale when a holiday is added or removed for a date that has existing approvals; stale approvals remain in effect until a manager explicitly re-reviews
FR43: Employee can retrieve their own attendance report for a specified date range, including per-schedule segment breakdowns and approval status
FR44: Manager can retrieve attendance reports for their direct reports only
FR45: HR Admin can retrieve attendance reports for all employees
FR46: The system enforces report scope server-side from the caller's JWT claims; no request parameter can expand a caller's scope beyond their role boundary
FR47: HR Admin can view all system feature flags and their current values
FR48: HR Admin can toggle the HolidayPayWithinScheduledHours flag; when disabled, Regular Holiday Paid Hours and Special Holiday Paid Hours (and their night diff variants) are suppressed while OT types remain unaffected
FR49: All feature flag changes are written to an immutable audit log with actor, timestamp, old value, and new value
FR50: The system authenticates all API requests via JWT Bearer tokens; requests with missing, expired, or invalid tokens are rejected before processing
FR51: The system derives user identity and access scope exclusively from JWT claims (sub, role); request parameters cannot substitute for or override claims
FR52: All API routes are accessible under the /api/v1/ path prefix; breaking changes require a new version
FR53: All error responses follow RFC 7807 Problem Details format with a distinct type URI per error category
FR54: The system enforces rate limits per authenticated user with a standard policy (all endpoints) and a stricter bulk policy (batch approval and report endpoints); exceeded limits return structured error responses with Retry-After
FR55: A test JWT issuer endpoint is available for development and testing purposes and is documented as non-production
FR56: All endpoints, request/response schemas, the complete 40-type classification enum, and all Problem Details type URIs are documented in Swagger/OpenAPI with examples

### NonFunctional Requirements

NFR-P1: The individual computation endpoint (GET /api/v1/schedules/{id}/computation) responds in < 200ms at the 95th percentile under normal operating conditions
NFR-P2: The attendance report endpoint completes for a 31-day range across up to 100 employees in < 5 seconds at the 95th percentile
NFR-P3: Bulk approval batch commit for 100 employees completes in < 3 seconds at the 95th percentile
NFR-P4: All non-computation endpoints respond in < 500ms at the 95th percentile
NFR-S1: All API traffic is served over HTTPS; HTTP connections are rejected at the transport layer
NFR-S2: JWT tokens must be RS256-signed; alg:none and symmetric-algorithm tokens are rejected at middleware before any business logic executes
NFR-S3: JWT exp, iss, and aud claims are validated on every authenticated request; expired or mismatched tokens are rejected with 401
NFR-S4: Employee compensation data is only accessible within the caller's role-defined scope, enforced server-side on every request
NFR-S5: Rate limiting is enforced per authenticated sub claim, not per IP; switching IPs does not reset a user's rate limit window
NFR-S6: All database interactions use ORM-generated or parameterized queries; raw string concatenation into SQL is prohibited throughout the codebase
NFR-R1: OT approval commits are fully atomic — all staged actions persist together or none do; the database must not contain a partial commit state
NFR-R2: Audit log records for OT commits and feature flag changes are written within the same database transaction as the committed action
NFR-R3: The computation engine is deterministic — identical inputs (schedule, logs, approvals, holiday calendar, feature flags, server time) always produce identical output regardless of call order or concurrency
NFR-R4: Idempotent endpoints deduplicate within the 5-minute window regardless of concurrent requests from the same client
NFR-R5: Server clock is NTP-synchronized; bare OS system time without NTP is not acceptable for production deployment
NFR-T1: All 40 classification types have at least one automated positive test case and one automated negative test case in the test suite
NFR-T2: All 8 computation engine invariants have dedicated test cases verifying that violations are detected, all violations are collected before rejection, and the invalid result cannot be constructed
NFR-T3: All 8 user journey scenarios have corresponding integration test cases with exact expected segment output assertions (classification, duration, approval status per segment)
NFR-T4: The computation engine is testable without a live database, HTTP server, or real clock — all external dependencies are injected through the 5 defined domain interfaces

### Additional Requirements

- Project initialised via `dotnet new webapi --use-controllers --name ph-payroll-time-api` then restructured into a 4-project solution (Domain, Application, Infrastructure, Api) plus 3 test projects (Domain.Tests, Application.Tests, Integration.Tests)
- PostgreSQL database via Docker (`postgres:16-alpine`); EF Core 8.0.25 Code First migrations with `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.8`
- Global EF Core `DateTimeOffset` → `timestamptz` convention applied in `OnModelCreating`; `DateTime` prohibited throughout
- `WorkSchedulePattern.RestDays` stored as PostgreSQL `integer[]` with explicit EF value converter (no junction table)
- JWT RS256 via `Microsoft.AspNetCore.Authentication.JwtBearer`; `ValidAlgorithms = ["RS256"]` in `TokenValidationParameters`; `alg:none` rejected at middleware
- Rate limiting via built-in `Microsoft.AspNetCore.RateLimiting` middleware: standard policy 300 req/min and bulk policy 20 req/min, both keyed by `sub` claim
- API versioning via `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer`; all routes under `/api/v1/`
- RFC 7807 Problem Details via `ExceptionHandlerMiddleware`; all `type` URIs centralised in `ProblemTypes` static class
- Serilog structured logging via `Serilog.AspNetCore`; console + rolling file sinks
- `IMemoryCache` for idempotency key deduplication (5-minute sliding TTL); cache key = `SHA256(method + path + Idempotency-Key header)`
- Mandatory `Program.cs` middleware pipeline order: ExceptionHandler → HttpsRedirection → SerilogRequestLogging → Authentication → Authorization → RateLimiter → IdempotencyMiddleware → MapControllers (Authentication must precede RateLimiter so the `sub` claim is available for rate-limit keying per NFR-S5)
- Docker Compose file with two services: `postgres:16-alpine` and API image; `docker compose up --build` serves as portfolio demo
- CQRS dispatch via raw DI handlers (`ICommandHandler<T>` / `IQueryHandler<T,R>`); no MediatR
- `ComputationResult` constructor enforces all 8 invariants and collects all violations before throwing (`ComputationInvariantException` with `List<string> Violations`)
- Swagger/OpenAPI via `Swashbuckle.AspNetCore`; `JsonStringEnumConverter` applied globally; all 40 enum types enumerated in schema
- `AsNoTracking()` mandatory on all read queries; audit records written in same DB transaction as their triggering write

### UX Design Requirements

N/A — API-only project; no frontend or UX specification.

### FR Coverage Map

```
FR1:  Epic 2 — Create employee profile
FR2:  Epic 2 — Update employee profile
FR3:  Epic 2 — Create shift schedule (cross-date, break window)
FR4:  Epic 2 — Update/delete shift schedule
FR5:  Epic 2 — Overlap prevention (409 Conflict)
FR6:  Epic 2 — Assign work schedule pattern with effective/expiry date
FR7:  Epic 2 — Update work schedule pattern dates
FR8:  Epic 2 — Employee views own schedules
FR9:  Epic 4 — Employee submits time log at server time only
FR10: Epic 4 — Manager/HR submits time log for any employee
FR11: Epic 4 — Manager/HR views time logs in date range
FR12: Epic 4 — Employee views own time logs
FR13: Epic 4 — Time log idempotency (5-minute window, Idempotency-Key header)
FR14: Epic 5 — 40-type classification per schedule minute
FR15: Epic 5 — Break deduction as clocked-interval intersection with break window
FR16: Epic 5 — Night diff split at 10pm/6am boundaries
FR17: Epic 5 — Rest day status from WorkSchedulePattern.RestDays
FR18: Epic 5 — Holiday/rest day rules governed by shift start date
FR19: Epic 5 — Full log pairing algorithm (same-day + prior-day IN, OUT fencing)
FR20: Epic 5 — IN_PROGRESS when no valid OUT before schedule end
FR21: Epic 5 — ABSENT when no valid OUT at or after schedule end
FR22: Epic 5 — REGULAR_HOLIDAY_REST with no schedule and no logs
FR23: Epic 5 — Special Non-Working Holiday: no result with no schedule/logs
FR24: Epic 5 — HR review flag when logs present regardless of approval
FR25: Epic 5 — 8 invariant enforcement, all violations collected before rejection
FR26: Epic 6 — Manager views pending Holiday Schedule Approvals
FR27: Epic 6 — Manager bulk approve/reject holiday schedules (≤100 employees)
FR28: Epic 6 — Manager views pending Rest Day Schedule Approvals
FR29: Epic 6 — Manager bulk approve/reject rest day schedules (≤100 employees)
FR30: Epic 6 — Combined approval when rest day + non-working holiday coincide
FR31: Epic 6 — Manager views pending OT Approvals with staged segments
FR32: Epic 6 — Manager stages OT adjustment actions
FR33: Epic 6 — Manager removes staged OT action
FR34: Epic 6 — Automatic OT segment cascade on duration boundary crossing
FR35: Epic 6 — Atomic OT commit with pre-commit conflict validation
FR36: Epic 6 — Holiday/Rest Day Approval prerequisite gate for OT Approval
FR37: Epic 6 — OT commit idempotency deduplication
FR38: Epic 6 — Immutable audit record on OT commit
FR39: Epic 3 — All authenticated users view holiday calendar
FR40: Epic 3 — HR Admin CRUD holiday entries (REGULAR/SPECIAL_NON_WORKING/SPECIAL_WORKING/NONE)
FR41: Epic 3 — Special Working Day treated as ordinary working day (no pay premium)
FR42: Epic 6 — Stale flag on committed OT approvals when holiday changes
FR43: Epic 7 — Employee retrieves own attendance report
FR44: Epic 7 — Manager retrieves direct-reports attendance reports
FR45: Epic 7 — HR Admin retrieves all-employees attendance reports
FR46: Epic 7 — JWT-enforced report scope (no parameter override)
FR47: Epic 8 — HR Admin views all feature flags and current values
FR48: Epic 8 — HR Admin toggles HolidayPayWithinScheduledHours flag
FR49: Epic 8 — Feature flag immutable audit log (actor, timestamp, old/new value)
FR50: Epic 1 — JWT Bearer auth, reject missing/expired/invalid tokens
FR51: Epic 1 — Identity from JWT claims only (sub, role); no parameter override
FR52: Epic 1 — All routes under /api/v1/
FR53: Epic 1 — RFC 7807 Problem Details, distinct type URI per error category
FR54: Epic 1 — Rate limiting per sub claim (standard 300/min + bulk 20/min)
FR55: Epic 1 — Test JWT issuer endpoint (POST /api/v1/auth/token — non-production only)
FR56: Epic 1 — Swagger/OpenAPI with all 40 enum types enumerated as strings
```

### Journey-to-Epic Mapping

| Journey | Description | First Fully Demonstrable After |
|---|---|---|
| J1: Ana — Early OT crossing midnight into Regular Holiday | Employee submits OT before midnight; system splits at midnight; holiday rules apply after | **Epic 6** |
| J2: Marco — Regular Holiday night shift | Employee works night shift on a holiday; night diff + holiday multipliers combine | **Epic 6** |
| J3: Carlos — Holiday Schedule Approval | Manager approves holiday schedule for an employee working on a Regular Holiday | **Epic 6** |
| J4: Carlos — OT Adjustment Cascade | Manager reduces OT duration past midnight boundary; cascade removes and reinstates segments | **Epic 6** |
| J5: Carlos — Missed Punch Correction | Manager submits corrective OUT log after employee forgot to clock out | **Epic 6** |
| J6: Maria — HR Onboarding + Feature Flag Toggle | HR Admin creates employee, configures holiday calendar, toggles HolidayPayWithinScheduledHours | **Epic 8** |
| J7: Dev Team — Swagger Discovery | Developer opens Swagger UI, inspects all 40 classification types and Problem Details schemas | **Epic 1** |
| J8: Attendance Report — Role-Scoped Access | Employee, Manager, HR Admin each retrieve reports; JWT scope enforcement verified | **Epic 7** |

**Note:** Journey 7 is demonstrable immediately after Epic 1. Journeys 1–5 require the full approval workflow (Epic 6). Journey 8 requires reporting (Epic 7). Journey 6 requires feature flags (Epic 8). Zero journeys beyond J7 are fully demonstrable before Epic 6 completes — this is the expected build sequence for a computation-heavy system.

---

## Epic List

### Epic 1: API Foundation & Cross-Cutting Infrastructure
The API is runnable, secured with JWT RS256, rate-limited per `sub` claim, versioned under `/api/v1/`, RFC 7807-compliant, Swagger-documented, and seeded with portfolio demo data. Every future endpoint inherits this backbone. Journey 7 (Swagger discovery) is demonstrable at the end of this epic.

**FRs covered:** FR50, FR51, FR52, FR53, FR54, FR55, FR56

**Implementation notes:**
- Scaffold 7-project solution (Domain, Application, Infrastructure, Api + 3 test projects); register `WebApplicationFactory` base class in Integration.Tests
- Define all domain entity classes in Epic 1 domain spike; apply single `InitialSchema` baseline EF migration covering all entities from all epics
- Register `HardcodedFeatureFlagProvider` (all flags enabled) in DI as `IFeatureFlagProvider` until Epic 8 switches to `DbFeatureFlagProvider`
- Add Roslyn analyzer or reflection-based unit test asserting no entity property uses `DateTime` (enforcement of DateTimeOffset mandate)
- `IClockProvider` with `Asia/Manila` interpretation must be established as cross-cutting AC: all calendar-day boundary evaluations use Manila local time throughout all layers
- Idempotency middleware built in this epic but first integration-tested when POST endpoints exist (Epic 4); add a stub test endpoint in `Domain.Tests` to verify middleware independently
- Middleware pipeline order must be tested: assert `UseAuthentication` precedes `UseRateLimiter` (NFR-S5)
- `DataSeeder` class seeds demo data in `Development` environment: employees for all 3 roles, Regular Holiday + Special Non-Working Holiday entries, sample shift schedules spanning midnight and 10pm night-diff boundary, sample time logs matching Journey 1–8 scenarios

---

### Epic 2: Employee & Work Schedule Management
HR Admins and Managers can create and manage employee profiles, define shift schedules (including cross-date shifts and break windows), assign work schedule patterns with effective/expiry dates, and employees can view their own schedules.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR8

**Implementation notes:**
- Employee soft-delete (set `IsActive = false`) rather than hard-delete; prevents FK integrity issues when TimeLog records exist
- Schedule overlap validation returns 409 Conflict with Problem Details `type: conflict/overlapping-schedule`
- Bulk schedule pattern assignment for up to 100 employees must meet `<3s` NFR-P3 target; use EF Core batch insert strategy (not N+1 saves)
- `WorkSchedulePattern.RestDays` stored as PostgreSQL `integer[]` per architecture Gap 2 (EF value converter)

---

### Epic 3: Holiday Calendar Management
HR Admin can maintain the Philippine holiday calendar with all four HOLIDAY_TYPE values; all authenticated users can query holidays by date range. The `SPECIAL_WORKING` type is stored and returned but has no pay premium effect on computation.

**FRs covered:** FR39, FR40, FR41

**Implementation notes:**
- `HolidayType` enum values (`REGULAR`, `SPECIAL_NON_WORKING`, `SPECIAL_WORKING`, `NONE`) must align exactly with values used in Epic 5's computation classification logic — this mapping is load-bearing
- Duplicate holiday date handling: return 409 Conflict (not upsert)
- `FR41` (Special Working Day = ordinary working day) is enforced implicitly by the computation engine in Epic 5 — no special handling in holiday CRUD beyond correct type storage

---

### Epic 4: Time Log Capture
Employees can submit their own time logs (at server time only); Managers and HR Admins can submit time logs for any employee on any date; all parties can query relevant time logs; idempotency prevents duplicate submissions within the 5-minute window.

**FRs covered:** FR9, FR10, FR11, FR12, FR13

**Implementation notes:**
- `POST /api/v1/time-logs` requires `Idempotency-Key` header; the same SHA256 idempotency middleware from Epic 1 applies; idempotency key = `SHA256(POST + /api/v1/time-logs + Idempotency-Key header value)`
- Open clock-in (no matching OUT): valid at submission time; system returns `IN_PROGRESS` or `ABSENT` at computation time depending on server time vs. schedule end (Epic 5 behavior)
- Future-dated log entries: reject with 422 Unprocessable Entity
- Date range validation on query endpoints: `startDate <= endDate` required; `400 Bad Request` if violated; maximum range not capped (enforced by NFR-P4 `<500ms` response target)

---

### Epic 5: Attendance Computation Engine
The system classifies every minute of any schedule entry into one of 40 defined types per Philippine DOLE rules — incorporating time logs, holiday calendar, work schedule patterns, night differential boundaries (10pm–6am), the full log pairing algorithm, and all 8 computation invariants.

**FRs covered:** FR14, FR15, FR16, FR17, FR18, FR19, FR20, FR21, FR22, FR23, FR24, FR25

**Implementation notes:**
- Entry point: `ComputationEngine.cs` in `Domain/Services/`; `LogPairingService.cs` handles FR19 algorithm; both have zero infrastructure dependencies
- `ILogClaimTracker` (scoped per request via `InMemoryLogClaimTracker`) is implemented and fully exercised in this epic — it tracks claimed TimeLog IDs during a single `Compute()` call to prevent double-assignment across overlapping schedule entries
- Unit tests (`Domain.Tests/ComputationEngine/`) must be fully independent of DB, HTTP, and real clock — NSubstitute stubs for all 5 interfaces; these tests run before Epics 2–4 are complete
- Integration tests for the computation endpoint require Epics 2, 3, and 4 to be complete; these are the first Journey integration tests (partial coverage of J1–J5)
- The 40 classification types cluster into 7 test files (see architecture Directory Structure); each cluster covers one scenario group × regular + night-diff variants
- All 8 invariants enumerated in architecture Gap 3 must each have a dedicated positive test and a dedicated negative test in `InvariantEnforcementTests.cs`
- `ComputationEngine` receives `WorkSchedulePattern` as a direct method argument (not through a domain interface) — Application layer resolves it before calling the engine

---

### Epic 6: Approval Workflows
Managers can review, stage, and atomically commit holiday schedule approvals, rest day schedule approvals, and overtime approvals for their direct reports — with prerequisite gating, boundary-crossing cascade effects, idempotent commits, and immutable audit trails. Holiday calendar changes retroactively flag stale OT approvals.

**FRs covered:** FR26, FR27, FR28, FR29, FR30, FR31, FR32, FR33, FR34, FR35, FR36, FR37, FR38, FR42

**Implementation notes:**
- Approval state machine: `Pending → Approved | Rejected`; an approved request cannot be re-approved without being rejected first
- Concurrent approval conflict (two managers approve simultaneously): use EF Core optimistic concurrency (row version / `xmin` in PostgreSQL) to detect and reject the second commit with 409
- `OtApproval` records store status + committed segment overrides only — no pre-computed hours (ADR-001)
- FR42 (stale flagging): the `UpdateHoliday` and `DeleteHoliday` command handlers query existing `OtApproval` records for the affected date and set their `IsStale = true` flag; this story is part of this epic since `OtApproval` entity is defined here
- FR30 (combined rest day + non-working holiday): system issues a single `CombinedApprovalRequest` record; individual approval/rejection on either sub-type is rejected with 409
- Audit records written in same DB transaction as OT commit (NFR-R2)
- Journey integration tests J1–J5 must be complete and passing at the end of this epic

---

### Epic 7: Attendance Reporting
Employees, Managers, and HR Admins can retrieve attendance reports scoped strictly to their role — showing per-schedule segment classifications, durations, and approval statuses for any date range — with JWT-enforced scope boundaries and paginated results.

**FRs covered:** FR43, FR44, FR45, FR46

**Implementation notes:**
- Pagination: results paginated by `employeeId` cursor (not offset) for stable large result sets; `pageSize` defaults to 20 employees; `nextCursor` returned in response when more pages exist
- Date range validation: `startDate <= endDate` required; range capped at 366 days; 400 Bad Request if violated
- Report `<5s` for 31-day × 100-employee range (NFR-P2) is validated under this performance constraint with stateless recomputation; bulk-fetch all data in set-based queries before entering the computation loop (no query-inside-loop pattern)
- JWT scope enforcement is tested explicitly: Employee token returns only own data (403 for others), Manager token scoped to direct reports, HR Admin unrestricted
- Journey 8 (role-scoped attendance report) integration test must be complete and passing at the end of this epic

---

### Epic 8: Feature Flag Administration
HR Admin can view all system feature flags and their current values, toggle the `HolidayPayWithinScheduledHours` flag to suppress or restore holiday pay within scheduled hours, and access an immutable audit log of all flag changes. DI switches from `HardcodedFeatureFlagProvider` to `DbFeatureFlagProvider`.

**FRs covered:** FR47, FR48, FR49

**Implementation notes:**
- Replace `HardcodedFeatureFlagProvider` DI registration with `DbFeatureFlagProvider` in `InfrastructureServiceExtensions.cs`
- `FeatureFlag` entity seeded with default values matching `HardcodedFeatureFlagProvider` behavior (all enabled = `true`) so existing behavior is preserved at switchover
- Flag evaluation: DB value is authoritative; no environment variable override (keeps it simple for portfolio)
- Fail-open: if DB is unavailable during flag read, catch exception and return default `true` value (same as stub behavior) — documented as known behavior
- Feature flag audit records written in same DB transaction as toggle (NFR-R2)
- Journey 6 (HR onboarding + feature flag toggle) integration test must be complete and passing at the end of this epic

---

## Epic 1: API Foundation & Cross-Cutting Infrastructure

The API is runnable, secured with JWT RS256, rate-limited per `sub` claim, versioned under `/api/v1/`, RFC 7807-compliant, Swagger-documented, and seeded with portfolio demo data. Every future endpoint inherits this backbone. Journey 7 (Swagger discovery) is demonstrable at the end of this epic.

### Story 1.1: Solution Scaffold, Domain Entity Foundation & EF Core Migration

As a developer,
I want the 7-project solution scaffolded with all domain entities defined and a single baseline EF Core migration applied,
So that all future epics inherit a stable, constraint-compliant data model without migration churn.

**Acceptance Criteria:**

**Given** the solution is opened and built
**When** all 7 projects are compiled
**Then** Domain, Application, Infrastructure, Api, Domain.Tests, Application.Tests, and Integration.Tests all build without errors
**And** project references follow clean architecture direction (Api → Application → Domain; Infrastructure → Domain; no Domain reference to Application or Infrastructure)

**Given** a reflection-based unit test in Domain.Tests runs
**When** all entity properties are inspected
**Then** no property uses `System.DateTime` (all timestamps are `DateTimeOffset`)
**And** the test fails the build if any `DateTime` property is found

**Given** the database is running
**When** `dotnet ef database update` is run
**Then** the `InitialSchema` migration applies all tables (Employee, ShiftSchedule, WorkSchedulePattern, TimeLog, HolidayEntry, OtApproval, FeatureFlag, AuditLog, and related) without error
**And** `WorkSchedulePattern.RestDays` is stored as PostgreSQL `integer[]` via an EF value converter
**And** all `DateTimeOffset` columns map to `timestamptz` via the global `OnModelCreating` convention

**Given** the Application layer
**When** `ICommandHandler<T>` and `IQueryHandler<T,R>` interfaces are defined
**Then** all handlers are resolvable from DI via assembly scanning
**And** no MediatR package is referenced anywhere in the solution

**Given** the application starts
**When** `IClockProvider` is resolved from DI
**Then** it returns current UTC time, and all calendar-day boundary evaluations use Asia/Manila local time interpretation

**Given** Integration.Tests
**When** `WebApplicationFactory<Program>` base class is registered
**Then** integration tests can spin up the full in-process API host

---

### Story 1.2: JWT RS256 Authentication & Test Token Issuer

As a developer / system operator,
I want all API requests authenticated via JWT RS256 Bearer tokens with a non-production test token issuer,
So that only authenticated requests reach business logic and developers can generate tokens without a real IdP.

**Acceptance Criteria:**

**Given** a request with no Authorization header
**When** any protected endpoint is called
**Then** the response is 401 Unauthorized with RFC 7807 Problem Details

**Given** a request with an expired JWT
**When** any protected endpoint is called
**Then** the response is 401 Unauthorized before any handler executes

**Given** a request with a JWT using alg:none or HS256
**When** the authentication middleware processes it
**Then** the token is rejected with 401
**And** `ValidAlgorithms = ["RS256"]` is enforced in `TokenValidationParameters` (NFR-S2)

**Given** a request with a valid RS256 JWT
**When** the middleware processes it
**Then** `sub` and `role` claims are extracted as the user's identity
**And** no request parameter can substitute for or override these claims (FR51)

**Given** a JWT with mismatched `iss` or `aud` claims
**When** any protected endpoint is called
**Then** the response is 401 Unauthorized (NFR-S3)

**Given** the application is running in Development environment
**When** `POST /api/v1/auth/token` is called with a `role` and `sub` payload
**Then** a valid RS256-signed JWT is returned
**And** the endpoint is marked non-production in Swagger documentation (FR55)

---

### Story 1.3: API Versioning & RFC 7807 Problem Details

As a developer / API consumer,
I want all routes versioned under `/api/v1/` and all error responses in RFC 7807 format,
So that breaking changes can be versioned independently and error handling is consistent for all consumers.

**Acceptance Criteria:**

**Given** any API endpoint
**When** the route is inspected
**Then** it is accessible under the `/api/v1/` path prefix (FR52)
**And** `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer` are the versioning packages used

**Given** an unhandled exception occurs in any handler
**When** `ExceptionHandlerMiddleware` processes it
**Then** the response has `Content-Type: application/problem+json`
**And** the body contains `type`, `title`, `status`, and `detail` per RFC 7807 (FR53)
**And** each error category has a distinct `type` URI defined in the `ProblemTypes` static class

**Given** a request for a non-existent route
**When** the router processes it
**Then** the response is 404 with RFC 7807 Problem Details

**Given** a request with invalid model input
**When** model validation fails
**Then** the response is 400 with RFC 7807 Problem Details including field-level error details

**Given** Serilog is configured
**When** the application starts
**Then** structured logs are written to both console and rolling file sinks
**And** each HTTP request is logged via `SerilogRequestLogging` middleware in the correct pipeline position

---

### Story 1.4: Rate Limiting per JWT Sub Claim

As a system operator,
I want all authenticated API traffic rate-limited per `sub` claim with separate standard and bulk policies,
So that no single user can exhaust API capacity regardless of IP address (NFR-S5).

**Acceptance Criteria:**

**Given** an authenticated user exceeds 300 requests within 60 seconds on standard endpoints
**When** the 301st request arrives
**Then** the response is 429 Too Many Requests with RFC 7807 Problem Details and a `Retry-After` header

**Given** an authenticated user exceeds 20 requests within 60 seconds on bulk endpoints (batch approval, report)
**When** the 21st request arrives
**Then** the response is 429 with RFC 7807 Problem Details and `Retry-After`

**Given** two requests from the same `sub` claim arrive from different IP addresses
**When** both are processed
**Then** they share the same rate limit counter (rate limiting is keyed by `sub`, not IP)

**Given** the `Program.cs` middleware pipeline
**When** middleware registration order is verified by a unit test
**Then** `UseAuthentication` precedes `UseRateLimiter`
**And** the full mandatory order is: ExceptionHandler → HttpsRedirection → SerilogRequestLogging → Authentication → Authorization → RateLimiter → IdempotencyMiddleware → MapControllers

---

### Story 1.5: Idempotency Middleware

As an API consumer,
I want idempotent POST endpoints to deduplicate requests sharing the same Idempotency-Key within a 5-minute window,
So that transient network retries cannot create duplicate records.

**Acceptance Criteria:**

**Given** a POST request with an `Idempotency-Key` header to an idempotency-enforced endpoint
**When** the middleware processes it
**Then** the cache key is computed as `SHA256(method + path + Idempotency-Key header value)`
**And** the response is stored in `IMemoryCache` with a 5-minute sliding TTL

**Given** a second POST request with the same `Idempotency-Key` within 5 minutes
**When** the middleware processes it
**Then** the cached response is returned immediately without invoking the handler

**Given** a POST request to an idempotency-enforced endpoint with no `Idempotency-Key` header
**When** the middleware processes it
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** a unit test in Domain.Tests targeting the idempotency middleware directly
**When** the test runs without a live HTTP server or database
**Then** it verifies cache key computation and deduplication behavior independently (NFR-T4)

---

### Story 1.6: Swagger/OpenAPI Documentation

As a developer / portfolio reviewer,
I want a Swagger UI documenting all endpoints, schemas, and the complete 40-type classification enum as strings,
So that Journey 7 (Swagger discovery) is fully demonstrable.

**Acceptance Criteria:**

**Given** the application is running
**When** `GET /swagger` is accessed
**Then** Swagger UI loads displaying all endpoints grouped under api version v1

**Given** the Swagger schema
**When** any enum type is inspected
**Then** all 40 classification type values are enumerated as strings (not integers)
**And** `JsonStringEnumConverter` is applied globally via `System.Text.Json` options (FR56)

**Given** the Swagger schema
**When** error response schemas are inspected
**Then** all RFC 7807 `type` URIs from `ProblemTypes` are documented with examples

**Given** the Swagger security configuration
**When** authentication requirements are inspected
**Then** JWT Bearer is defined as the security scheme
**And** all protected endpoints display the authorization requirement
**And** `POST /api/v1/auth/token` is documented as non-production

---

### Story 1.7: Data Seeder & Docker Compose Demo Setup

As a portfolio reviewer / developer,
I want `docker compose up --build` to start the full stack with all demo data seeded and Swagger accessible,
So that the portfolio demo runs end-to-end without any manual configuration.

**Acceptance Criteria:**

**Given** the Docker Compose file
**When** `docker compose up --build` is run
**Then** two services start: `postgres:16-alpine` and the built API image
**And** the API is reachable at its configured port with Swagger UI accessible

**Given** the application starts in Development environment
**When** `DataSeeder` runs on startup
**Then** employees for all 3 roles (Employee, Manager, HR Admin) are seeded with test JWT sub values
**And** at least one Regular Holiday and one Special Non-Working Holiday entry are seeded
**And** sample shift schedules spanning midnight and the 10pm night-diff boundary are seeded
**And** sample time logs covering Journey 1–8 scenarios are seeded

**Given** `HardcodedFeatureFlagProvider` is registered in DI as `IFeatureFlagProvider`
**When** any feature flag is evaluated
**Then** all flags return `true` (consistent with Epic 8's switchover default)

---

## Epic 2: Employee & Work Schedule Management

HR Admins and Managers can create and manage employee profiles, define shift schedules (including cross-date shifts and break windows), assign work schedule patterns with effective/expiry dates, and employees can view their own schedules.

### Story 2.1: Create & Update Employee Profile

As a Manager / HR Admin,
I want to create and update employee profiles,
So that employees are registered in the system with accurate data.

**Acceptance Criteria:**

**Given** a Manager or HR Admin with a valid JWT
**When** `POST /api/v1/employees` is called with valid employee data
**Then** a new employee record is created with `IsActive = true` and 201 Created is returned with the new employee ID

**Given** a Manager or HR Admin
**When** `POST /api/v1/employees` is called with missing required fields
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** an HR Admin with a valid JWT
**When** `PUT /api/v1/employees/{id}` is called with updated data
**Then** the employee record is updated and 200 OK is returned (FR2)

**Given** a Manager (not HR Admin)
**When** `PUT /api/v1/employees/{id}` is called
**Then** the response is 403 Forbidden (FR2 — update is HR Admin only)

**Given** an HR Admin
**When** `DELETE /api/v1/employees/{id}` is called
**Then** `IsActive` is set to `false` and 204 No Content is returned
**And** the employee record is not physically removed from the database

**Given** a non-existent employee ID
**When** any employee endpoint is called
**Then** the response is 404 Not Found with RFC 7807 Problem Details

**Given** an Employee role JWT
**When** `POST` or `PUT /api/v1/employees` is called
**Then** the response is 403 Forbidden

---

### Story 2.2: Create, Update & Delete Shift Schedules with Overlap Prevention

As a Manager / HR Admin,
I want to create, update, and delete shift schedules including cross-date shifts with break windows,
So that employees have accurate schedule records that the computation engine can process.

**Acceptance Criteria:**

**Given** a Manager or HR Admin
**When** `POST /api/v1/employees/{id}/schedules` is called with a shift starting on one calendar day and ending on the next
**Then** a cross-date shift schedule is created with correct `DateTimeOffset` start and end values
**And** 201 Created is returned

**Given** a Manager or HR Admin
**When** `POST /api/v1/employees/{id}/schedules` is called with a break window
**Then** the schedule is created with the break window start and end times stored correctly

**Given** a Manager or HR Admin
**When** `POST /api/v1/employees/{id}/schedules` is called with dates overlapping an existing schedule for that employee
**Then** the response is 409 Conflict with RFC 7807 Problem Details and `type: conflict/overlapping-schedule` (FR5)
**And** no schedule record is created

**Given** a Manager or HR Admin
**When** `PUT /api/v1/employees/{id}/schedules/{scheduleId}` is called with valid updated data
**Then** the schedule is updated and 200 OK is returned
**And** overlap validation is re-run against all other schedules for that employee

**Given** a Manager or HR Admin
**When** `DELETE /api/v1/employees/{id}/schedules/{scheduleId}` is called
**Then** the schedule record is removed and 204 No Content is returned

**Given** an Employee role JWT
**When** `POST`, `PUT`, or `DELETE` on any schedule endpoint is called
**Then** the response is 403 Forbidden

---

### Story 2.3: Assign & Manage Work Schedule Patterns

As a Manager / HR Admin,
I want to assign a work schedule pattern to an employee with an effective date and optional expiry date,
So that the system knows which days are rest days for attendance classification.

**Acceptance Criteria:**

**Given** a Manager or HR Admin
**When** `POST /api/v1/employees/{id}/schedule-patterns` is called with a valid pattern, effective date, and optional expiry date
**Then** the `WorkSchedulePattern` is assigned and 201 Created is returned
**And** `RestDays` is stored as PostgreSQL `integer[]` via the EF value converter (FR6)

**Given** a Manager or HR Admin
**When** bulk assignment is called for up to 100 employees in a single request
**Then** all assignments complete within 3 seconds (NFR-P3)
**And** EF Core batch insert strategy is used — no N+1 individual saves

**Given** a Manager or HR Admin
**When** `PUT /api/v1/employees/{id}/schedule-patterns/{patternId}` is called with an updated effective or expiry date
**Then** the pattern assignment dates are updated and 200 OK is returned (FR7)

**Given** a Manager or HR Admin
**When** `PUT` is called with an expiry date earlier than the effective date
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** a Manager or HR Admin
**When** a pattern with no expiry date is assigned
**Then** the assignment is open-ended and the system treats it as active indefinitely

**Given** an Employee role JWT
**When** any schedule-pattern write endpoint is called
**Then** the response is 403 Forbidden

---

### Story 2.4: Employee Views Own Schedules

As an Employee,
I want to view my own shift schedules,
So that I can see my assigned shifts and plan accordingly.

**Acceptance Criteria:**

**Given** an Employee with a valid JWT
**When** `GET /api/v1/employees/{id}/schedules` is called where `{id}` matches their own `sub` claim
**Then** their shift schedules are returned with 200 OK
**And** `AsNoTracking()` is used on the query

**Given** an Employee with a valid JWT
**When** `GET /api/v1/employees/{id}/schedules` is called for a different employee's `{id}`
**Then** the response is 403 Forbidden (NFR-S4 — JWT scope enforcement)

**Given** a Manager or HR Admin with a valid JWT
**When** `GET /api/v1/employees/{id}/schedules` is called for any employee
**Then** the schedules are returned with 200 OK

**Given** an employee with no schedules assigned
**When** `GET /api/v1/employees/{id}/schedules` is called
**Then** an empty array is returned with 200 OK

---

## Epic 3: Holiday Calendar Management

HR Admin can maintain the Philippine holiday calendar with all four HOLIDAY_TYPE values; all authenticated users can query holidays by date range. The `SPECIAL_WORKING` type is stored and returned but has no pay premium effect on computation.

### Story 3.1: HR Admin Manages Holiday Calendar Entries

As an HR Admin,
I want to add, update, and remove Philippine holiday calendar entries with a date, name, and holiday type,
So that the system has an accurate holiday calendar for attendance computation.

**Acceptance Criteria:**

**Given** an HR Admin with a valid JWT
**When** `POST /api/v1/holidays` is called with a date, name, and valid `HOLIDAY_TYPE`
**Then** a holiday entry is created and 201 Created is returned
**And** `HOLIDAY_TYPE` accepts exactly: `REGULAR`, `SPECIAL_NON_WORKING`, `SPECIAL_WORKING`, `NONE` (FR40)

**Given** an HR Admin
**When** `POST /api/v1/holidays` is called with a date that already has a holiday entry
**Then** the response is 409 Conflict with RFC 7807 Problem Details (not an upsert)

**Given** an HR Admin
**When** `PUT /api/v1/holidays/{date}` is called with an updated name or type
**Then** the holiday entry for that date is updated and 200 OK is returned

**Given** an HR Admin
**When** `DELETE /api/v1/holidays/{date}` is called
**Then** the holiday entry for that date is removed and 204 No Content is returned

**Given** an HR Admin
**When** `POST /api/v1/holidays` is called with an invalid `HOLIDAY_TYPE` value
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** a `SPECIAL_WORKING` holiday type is stored
**When** it is returned in any API response
**Then** `HOLIDAY_TYPE` is serialized as a string via `JsonStringEnumConverter`
**And** no pay premium logic is applied at this layer — enforcement is deferred to the computation engine in Epic 5 (FR41)

**Given** a Manager or Employee role JWT
**When** any holiday write endpoint (`POST`, `PUT`, `DELETE`) is called
**Then** the response is 403 Forbidden

---

### Story 3.2: Authenticated Users Query Holiday Calendar

As any authenticated user,
I want to query the holiday calendar filtered by date range,
So that I can view which days are holidays for planning and reference.

**Acceptance Criteria:**

**Given** any authenticated user
**When** `GET /api/v1/holidays?startDate=X&endDate=Y` is called with a valid date range
**Then** all holiday entries within the range are returned with 200 OK
**And** `AsNoTracking()` is used on the query (FR39)

**Given** any authenticated user
**When** `GET /api/v1/holidays?startDate=X&endDate=Y` is called where `startDate > endDate`
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** an unauthenticated request
**When** `GET /api/v1/holidays` is called
**Then** the response is 401 Unauthorized

**Given** no holidays exist in the requested date range
**When** `GET /api/v1/holidays?startDate=X&endDate=Y` is called
**Then** an empty array is returned with 200 OK

**Given** holidays of all four types exist in the date range
**When** `GET /api/v1/holidays` is called
**Then** all four `HOLIDAY_TYPE` values are returned serialized as strings

---

## Epic 4: Time Log Capture

Employees can submit their own time logs (at server time only); Managers and HR Admins can submit time logs for any employee on any date; all parties can query relevant time logs; idempotency prevents duplicate submissions within the 5-minute window.

### Story 4.1: Submit Time Log

As an Employee / Manager / HR Admin,
I want to submit time logs for attendance tracking,
So that the system has the clock events needed to compute attendance classifications.

**Acceptance Criteria:**

**Given** an Employee with a valid JWT
**When** `POST /api/v1/employees/{id}/logs` is called with a log type (IN or OUT) and no explicit timestamp
**Then** the log is recorded at the current server time and 201 Created is returned (FR9)
**And** the employee ID is derived from the JWT `sub` claim — no request parameter can override it

**Given** an Employee with a valid JWT
**When** `POST /api/v1/employees/{id}/logs` is called with an explicit timestamp that is in the future
**Then** the response is 422 Unprocessable Entity with RFC 7807 Problem Details

**Given** an Employee with a valid JWT
**When** `POST /api/v1/employees/{id}/logs` is called targeting a different employee's ID
**Then** the response is 403 Forbidden

**Given** a Manager or HR Admin with a valid JWT
**When** `POST /api/v1/employees/{id}/logs` is called for any employee with any past or current date
**Then** the log is recorded at the specified time and 201 Created is returned (FR10)

**Given** any authorized user submitting a time log
**When** `POST /api/v1/employees/{id}/logs` is called with an `Idempotency-Key` header
**Then** the idempotency middleware deduplicates subsequent requests with the same key within 5 minutes (FR13)
**And** the cache key is `SHA256(POST + /api/v1/employees/{id}/logs + Idempotency-Key header value)`

**Given** any authorized user
**When** `POST /api/v1/employees/{id}/logs` is called without an `Idempotency-Key` header
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** an unauthenticated request
**When** `POST /api/v1/employees/{id}/logs` is called
**Then** the response is 401 Unauthorized

---

### Story 4.2: Query Time Logs

As an Employee / Manager / HR Admin,
I want to query time logs within a date range,
So that I can review attendance clock events for myself or my reports.

**Acceptance Criteria:**

**Given** an Employee with a valid JWT
**When** `GET /api/v1/employees/{id}/logs?startDate=X&endDate=Y` is called where `{id}` matches their own `sub` claim
**Then** their own time logs within the date range are returned with 200 OK (FR12)
**And** `AsNoTracking()` is used on the query

**Given** an Employee with a valid JWT
**When** `GET /api/v1/employees/{id}/logs` is called for a different employee's `{id}`
**Then** the response is 403 Forbidden (NFR-S4)

**Given** a Manager or HR Admin with a valid JWT
**When** `GET /api/v1/employees/{id}/logs?startDate=X&endDate=Y` is called for any employee
**Then** time logs within the date range are returned with 200 OK (FR11)
**And** `AsNoTracking()` is used on the query

**Given** any authorized user
**When** `GET /api/v1/employees/{id}/logs` is called with `startDate > endDate`
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** an employee with no time logs in the requested range
**When** `GET /api/v1/employees/{id}/logs?startDate=X&endDate=Y` is called
**Then** an empty array is returned with 200 OK

---

## Epic 5: Attendance Computation Engine

The system classifies every minute of any schedule entry into one of 40 defined types per Philippine DOLE rules — incorporating time logs, holiday calendar, work schedule patterns, night differential boundaries (10pm–6am), the full log pairing algorithm, and all 8 computation invariants.

### Story 5.1: Log Pairing Algorithm

As the computation engine,
I want to pair IN and OUT time logs for each schedule entry using the full pairing algorithm,
So that the system can determine the actual clocked interval for each shift.

**Acceptance Criteria:**

**Given** a schedule entry and a set of time logs
**When** `LogPairingService.Pair()` is called
**Then** it first searches for an IN log on the same calendar day as the schedule start
**And** if not found, it looks for the earliest valid IN log from the immediately prior calendar day (FR19)

**Given** an IN log found from the prior calendar day
**When** OUT log candidates are evaluated
**Then** an OUT log is excluded if it falls within or after the next schedule's time range
**And** an IN log is excluded if it falls within or before another schedule's time range

**Given** a valid IN log is found but no valid OUT log exists
**When** server time (via `IClockProvider`) is before the schedule end time
**Then** `LogPairingService` returns `IN_PROGRESS` as the sole result for that schedule entry (FR20)

**Given** a valid IN log is found but no valid OUT log exists
**When** server time is at or after the schedule end time
**Then** `LogPairingService` returns `ABSENT` (FR21)

**Given** no valid IN log exists for the schedule entry
**When** `LogPairingService.Pair()` is called
**Then** it returns `ABSENT`

**Given** `ILogClaimTracker` is scoped per computation request via `InMemoryLogClaimTracker`
**When** a `TimeLog` ID is claimed by one schedule entry
**Then** it cannot be claimed again by any other overlapping schedule entry within the same `Compute()` call

**Given** the system `OTLookbackDays` is configured (default = 1, maximum = 3)
**When** `LogPairingService` searches for a prior-day IN log
**Then** it searches back only `OTLookbackDays` calendar days
**And** logs from further back than `OTLookbackDays` prior are not considered as valid IN candidates

**Given** the unit test suite in Domain.Tests
**When** `LogPairingService` tests run
**Then** they use NSubstitute stubs for `IClockProvider` with no DB, HTTP, or real clock dependencies (NFR-T4)

---

### Story 5.2: Core Classification Engine & Break Deduction

As the computation engine,
I want to classify every minute of a schedule entry into one of 40 defined types based on schedule boundaries, time logs, and work schedule pattern,
So that the system produces accurate attendance records per Philippine DOLE rules.

**Acceptance Criteria:**

**Given** a paired clocked interval for a schedule entry
**When** `ComputationEngine.Compute()` is called
**Then** every minute within the schedule is classified into exactly one of the 40 defined types (FR14)
**And** no minute belongs to more than one classification segment

**Given** a schedule entry with a break window
**When** the employee's clocked interval intersects the break window
**Then** the intersection duration is deducted as break time and those minutes are not classified as worked time (FR15)

**Given** the employee's clocked interval ends before the break window starts
**When** break deduction is computed
**Then** zero minutes are deducted (no break overlap)

**Given** the employee's clocked interval ends during the break window
**When** break deduction is computed
**Then** only the elapsed portion of the break window up to the OUT time is deducted (partial overlap)

**Given** the employee's clocked interval falls entirely within the break window
**When** break deduction is computed
**Then** all elapsed minutes are deducted and net compensable minutes equal zero
**And** this is a valid result — zero-duration compensable output must not throw or fail invariant checks

**Given** break deduction is applied to a schedule entry
**When** the classified segments are produced
**Then** break deduction applies only within Normal Paid Hours and Holiday Paid Hours segments
**And** OT segments (Early OT, Normal OT, and Holiday OT variants) are never reduced by the break window

**Given** an employee's active `WorkSchedulePattern`
**When** the shift date falls on a day listed in `RestDays`
**Then** the system classifies the shift as a rest day regardless of whether a schedule entry exists (FR17)
**And** rest day status is never inferred from shift absence alone

**Given** a cross-date shift starting on one calendar day and ending on the next
**When** holiday and rest day classification rules are applied
**Then** the rules applicable at the shift start date govern the entire shift (FR18)
**And** segments crossing midnight inherit the shift start date's holiday and rest day rules

**Given** `ComputationEngine.Compute()` called with identical inputs (schedule, logs, approvals, holiday calendar, feature flags, server time)
**When** called multiple times in any order or concurrently
**Then** it always produces identical output (NFR-R3 — deterministic)

**Given** the unit test suite in Domain.Tests
**When** `ComputationEngine` classification tests run
**Then** they use NSubstitute stubs for all required domain interfaces (`IClockProvider`, `IHolidayCalendar`, `IWorkSchedulePatternRepository`, `ILogClaimTracker`, `IFeatureFlagProvider`, `IHolidayApprovalRepository`) with no DB or HTTP dependencies (NFR-T4)

---

### Story 5.3: Night Differential Classification

As the computation engine,
I want to split any classified interval spanning the night differential window (10pm–6am) at its boundaries,
So that minutes inside the window receive the correct night differential classification variant.

**Acceptance Criteria:**

**Given** a classified segment that spans the 10pm Manila time boundary
**When** `ComputationEngine` processes it
**Then** the segment is split at exactly 10pm
**And** the portion before 10pm retains the base classification
**And** the portion from 10pm onward receives the night differential variant (FR16)

**Given** a classified segment that spans the 6am Manila time boundary
**When** `ComputationEngine` processes it
**Then** the segment is split at exactly 6am
**And** the portion before 6am retains the night differential variant
**And** the portion from 6am onward receives the base classification

**Given** a segment entirely within the 10pm–6am window
**When** `ComputationEngine` processes it
**Then** the entire segment receives the night differential variant

**Given** a segment entirely outside the 10pm–6am window
**When** `ComputationEngine` processes it
**Then** no night differential variant is applied

**Given** the unit test suite in Domain.Tests
**When** night differential tests run
**Then** the following scenarios each have a positive and negative test: segment crossing 10pm boundary, segment spanning midnight within window, segment crossing 6am boundary, segment entirely within window
**And** all tests use NSubstitute stubs with no infrastructure dependencies (NFR-T4)

---

### Story 5.4: Holiday Classification & HR Review Flag

As the computation engine,
I want to correctly classify holiday scenarios and flag results for HR review when time logs are present,
So that Philippine holiday pay rules are applied accurately.

**Acceptance Criteria:**

**Given** a Regular Holiday date with no approved schedule and no time logs
**When** `ComputationEngine.Compute()` is called
**Then** it returns `REGULAR_HOLIDAY_REST` as the sole classification (FR22)

**Given** a Special Non-Working Holiday date with no approved schedule and no time logs
**When** `ComputationEngine.Compute()` is called
**Then** it returns no result (empty) — no pay, no deduction per RA 9492 (FR23)

**Given** a Special Working Day date
**When** `ComputationEngine.Compute()` is called
**Then** the date is treated as an ordinary working day with no pay premium (FR41)

**Given** any schedule entry where time logs are present regardless of approval status
**When** `ComputationEngine.Compute()` is called
**Then** the computation result is flagged for HR review (FR24)
**And** statutory compensation rates are applied to the flagged result

**Given** the unit test suite in Domain.Tests
**When** holiday classification tests run
**Then** `REGULAR`, `SPECIAL_NON_WORKING`, and `SPECIAL_WORKING` holiday types each have at least one positive and one negative test case (NFR-T1)
**And** all tests use NSubstitute stubs with no infrastructure dependencies (NFR-T4)

---

### Story 5.5: Invariant Enforcement, Computation Endpoint & Integration Tests

As a system / API consumer,
I want the computation engine to enforce all 8 invariants and expose a performant API endpoint,
So that invalid results are never returned and attendance data is accessible within response time targets.

**Acceptance Criteria:**

**Given** a `ComputationResult` is constructed
**When** any of the 8 invariants is violated (Minute Conservation, No Overlap, No Zero-Duration, Classification Exclusivity, Regular Holiday Gate, Special Holiday Gate, OT Bounds, Schedule-Hours Bounds)
**Then** a `ComputationInvariantException` is thrown with a `List<string> Violations` containing all violations (FR25)
**And** all violations are collected before the exception is thrown — not fail-fast

**Given** a `ComputationResult` constructed with all invariants satisfied
**When** the result is returned
**Then** no exception is thrown

**Given** an authenticated user calls `GET /api/v1/schedules/{id}/computation`
**When** the computation completes
**Then** the response is 200 OK with per-segment classifications, durations, and approval status
**And** `AsNoTracking()` is used on all read queries feeding the engine

**Given** the computation endpoint under normal operating conditions
**When** response times are measured
**Then** p95 response time is under 200ms (NFR-P1)

**Given** the unit test suite in Domain.Tests
**When** `InvariantEnforcementTests` run
**Then** each of the 8 invariants has a dedicated positive test (valid result passes) and a dedicated negative test (violation is detected and collected) (NFR-T2)
**And** all 40 classification types have at least one positive and one negative test case across the 7 classification test clusters (NFR-T1)

**Given** the integration test suite in Integration.Tests with Epics 1–4 complete
**When** Journey integration tests run
**Then** Journey J1 (Ana — early OT crossing midnight into Regular Holiday) passes with exact expected segment output (NFR-T3)
**And** Journey J2 (Marco — Regular Holiday night shift) passes
**And** Journey J3 (Carlos — Holiday Schedule Approval) passes
**And** Journey J4 (Carlos — OT Adjustment Cascade) passes
**And** Journey J5 (Carlos — Missed Punch Correction) passes

---

## Epic 6: Approval Workflows

Managers can review, stage, and atomically commit holiday schedule approvals, rest day schedule approvals, and overtime approvals for their direct reports — with prerequisite gating, boundary-crossing cascade effects, idempotent commits, and immutable audit trails. Holiday calendar changes retroactively flag stale OT approvals.

### Story 6.1: Holiday Schedule Approval

As a Manager,
I want to view and bulk approve or reject holiday schedule approval requests for my direct reports,
So that employees working on holidays receive proper authorization.

**Acceptance Criteria:**

**Given** a Manager with a valid JWT
**When** `GET /api/v1/approvals/holiday-schedules?status=pending` is called
**Then** only pending Holiday Schedule Approval requests scoped to the Manager's direct reports are returned (FR26)
**And** requests for employees outside the Manager's direct reports are excluded

**Given** a Manager
**When** bulk approve or reject is submitted for up to 100 employee approval IDs
**Then** all specified approvals are updated atomically and 200 OK is returned (FR27)
**And** the operation completes within 3 seconds (NFR-P3)

**Given** a Manager
**When** bulk approve or reject is submitted with more than 100 employee IDs
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** an approval already in `Approved` or `Rejected` state
**When** a Manager attempts to re-approve it without first rejecting
**Then** the response is 409 Conflict with RFC 7807 Problem Details

**Given** two Managers attempt to approve the same request simultaneously
**When** the second commit arrives
**Then** EF Core optimistic concurrency (`xmin`) detects the conflict and returns 409 Conflict

**Given** an Employee or HR Admin role JWT
**When** any Holiday Schedule Approval write endpoint is called
**Then** the response is 403 Forbidden

---

### Story 6.2: Rest Day Schedule Approval & Combined Request

As a Manager,
I want to view and bulk approve or reject rest day schedule approval requests, with combined requests issued when a rest day coincides with a non-working holiday,
So that rest day work authorization is correctly managed without split decisions on combined entitlements.

**Acceptance Criteria:**

**Given** a Manager with a valid JWT
**When** `GET /api/v1/approvals/rest-day-schedules?status=pending` is called
**Then** only pending Rest Day Schedule Approval requests for the Manager's direct reports are returned (FR28)

**Given** a Manager
**When** bulk approve or reject is submitted for up to 100 employee rest day approvals
**Then** all are processed atomically and 200 OK is returned (FR29)

**Given** a rest day date that coincides with a Special Non-Working Holiday
**When** the system generates the approval request
**Then** a single `CombinedApprovalRequest` is issued covering both rest day schedule and holiday entitlement (FR30)

**Given** a `CombinedApprovalRequest` exists
**When** a Manager attempts to approve or reject only the rest day component or only the holiday component independently
**Then** the response is 409 Conflict with RFC 7807 Problem Details (FR30)

**Given** two Managers attempt to approve the same rest day request simultaneously
**When** the second commit arrives
**Then** EF Core optimistic concurrency (`xmin`) detects the conflict and returns 409 Conflict

**Given** an Employee or HR Admin role JWT
**When** any Rest Day Schedule Approval write endpoint is called
**Then** the response is 403 Forbidden

---

### Story 6.3: OT Approval — View & Stage Actions

As a Manager,
I want to view pending OT approval requests and stage adjustment actions before committing,
So that I can review and adjust overtime segment classifications without immediately affecting committed state.

**Acceptance Criteria:**

**Given** a Manager with a valid JWT
**When** `GET /api/v1/approvals/ot?status=pending` is called
**Then** only pending OT Approval requests for the Manager's direct reports are returned (FR31)
**And** all staged segment classifications and durations are included in the response

**Given** a Manager
**When** a stage action (reduce duration, override classification, or reject) is submitted for an OT segment
**Then** the staged action is recorded and 200 OK is returned (FR32)
**And** the staged action is not yet committed to the `OtApproval` record

**Given** a segment that already has a staged action
**When** a Manager submits a new stage action for the same segment
**Then** the new staged action replaces the existing one

**Given** a Manager
**When** `DELETE /api/v1/approvals/ot/staged/{actionId}` is called
**Then** the staged action is removed and 204 No Content is returned (FR33)
**And** the segment reverts to its original pre-staged state

**Given** an Employee or HR Admin role JWT
**When** any OT Approval endpoint is called
**Then** the response is 403 Forbidden

---

### Story 6.4: OT Approval — Cascade Effects

As the approval system,
I want to automatically remove and reinstate OT segment classifications when a manager reduces OT duration past a classification boundary,
So that staged actions always reflect valid DOLE-compliant segment states before commit.

**Acceptance Criteria:**

**Given** a Manager stages a reduce-duration action on an OT segment
**When** the reduced duration crosses the midnight boundary
**Then** the system automatically removes the post-midnight segment classification (FR34)
**And** reinstates the pre-midnight base classification for the affected portion

**Given** a Manager stages a reduce-duration action on an OT segment
**When** the reduced duration crosses the 10pm night differential threshold
**Then** the system automatically removes the night differential variant classification
**And** reinstates the base classification for the affected portion

**Given** cascading changes are triggered by a duration reduction
**When** the cascade is applied
**Then** all affected segment classifications are updated atomically in the staged state
**And** the Manager can view all updated staged segments before committing

**Given** a Manager removes a staged reduce-duration action
**When** `DELETE /api/v1/approvals/ot/staged/{actionId}` is called
**Then** all cascade effects from that action are reversed
**And** the segments return to their pre-staged state

---

### Story 6.5: OT Approval — Atomic Commit, Idempotency & Audit

As a Manager,
I want to atomically commit all staged OT actions for one or more employees with prerequisite gating and idempotency,
So that OT approvals are conflict-safe, never partially applied, and fully auditable.

**Acceptance Criteria:**

**Given** a Manager has staged OT actions for one or more employees
**When** `POST /api/v1/approvals/ot/commit` is called
**Then** the system validates all staged actions against current computation state before committing (FR35)
**And** if any conflict is detected the entire batch is rejected with 409 Conflict and no changes are persisted (NFR-R1)

**Given** all staged OT actions are valid
**When** the commit is processed
**Then** all staged actions persist atomically — all succeed or none do (NFR-R1)
**And** the operation completes within 3 seconds for up to 100 employees (NFR-P3)

**Given** an OT commit is attempted for a date where a Holiday Schedule Approval or Rest Day Schedule Approval is still pending
**When** the commit request is processed
**Then** the response is 409 Conflict — holiday and rest day approvals are prerequisite gates (FR36)

**Given** a commit request with an `Idempotency-Key` header
**When** a second request with the same key arrives within the deduplication window
**Then** only the first is processed; the second returns the cached response without re-executing (FR37)

**Given** a successful OT commit
**When** the commit transaction completes
**Then** an immutable audit record is written in the same database transaction containing all staged actions, cascade effects, actor `sub` claim, and timestamp (FR38, NFR-R2)

**Given** two Managers attempt to commit for the same employee simultaneously
**When** the second commit arrives
**Then** EF Core optimistic concurrency (`xmin`) detects the conflict and returns 409 Conflict

---

### Story 6.6: Stale OT Flag on Holiday Changes

As the system,
I want to flag committed OT approvals as stale when a holiday is added, updated, or removed for a date with existing approvals,
So that Managers are alerted to re-review approvals that may be affected by calendar changes.

**Acceptance Criteria:**

**Given** committed OT approvals exist for a date
**When** an HR Admin adds a holiday entry for that date
**Then** all `OtApproval` records for that date have `IsStale = true` set within the same transaction as the holiday creation (FR42)

**Given** committed OT approvals exist for a date
**When** an HR Admin deletes a holiday entry for that date
**Then** all `OtApproval` records for that date have `IsStale = true` set within the same transaction as the holiday deletion

**Given** committed OT approvals exist for a date
**When** an HR Admin updates the holiday type for that date
**Then** all `OtApproval` records for that date have `IsStale = true` set

**Given** `OtApproval` records with `IsStale = true`
**When** a Manager views OT approvals
**Then** stale approvals are indicated in the response
**And** they remain in effect until a Manager explicitly re-reviews — they are not automatically reversed

**Given** the integration test suite in Integration.Tests
**When** Journey tests J1 through J5 run at the end of Epic 6
**Then** all five journeys pass with exact expected segment output assertions (NFR-T3)

---

## Epic 7: Attendance Reporting

Employees, Managers, and HR Admins can retrieve attendance reports scoped strictly to their role — showing per-schedule segment classifications, durations, and approval statuses for any date range — with JWT-enforced scope boundaries and paginated results.

### Story 7.1: Attendance Report — Scoped Retrieval & JWT Enforcement

As an Employee / Manager / HR Admin,
I want to retrieve attendance reports scoped strictly to my role,
So that I can view per-schedule segment classifications, durations, and approval statuses without accessing data outside my authorized scope.

**Acceptance Criteria:**

**Given** an Employee with a valid JWT
**When** `GET /api/v1/reports/attendance?startDate=X&endDate=Y` is called
**Then** the report returns only the calling employee's own attendance data (FR43)
**And** scope is derived exclusively from the JWT `sub` claim — no request parameter can expand it (FR46)

**Given** an Employee with a valid JWT
**When** `GET /api/v1/reports/attendance` is called with another employee's ID in any parameter
**Then** the response is 403 Forbidden (FR46)

**Given** a Manager with a valid JWT
**When** `GET /api/v1/reports/attendance?startDate=X&endDate=Y` is called
**Then** the report returns attendance data only for the Manager's direct reports (FR44)
**And** employees outside the Manager's direct reports are excluded from the response

**Given** an HR Admin with a valid JWT
**When** `GET /api/v1/reports/attendance?startDate=X&endDate=Y` is called
**Then** the report returns attendance data for all employees in the system (FR45)

**Given** any authorized user
**When** the report is returned
**Then** it includes per-schedule segment breakdowns with classification type, duration, and approval status for each segment

**Given** any authorized user
**When** `GET /api/v1/reports/attendance` is called with `startDate > endDate`
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** any authorized user
**When** `GET /api/v1/reports/attendance` is called with a date range exceeding 366 days
**Then** the response is 400 Bad Request with RFC 7807 Problem Details

**Given** the `HolidayPayWithinScheduledHours` flag is toggled while a bulk report request is in flight
**When** the report computation processes multiple employees
**Then** the flag value is snapshotted once at request start and applied uniformly to all computations in that request
**And** the mid-request flag change does not affect the in-flight report results

**Given** an unauthenticated request
**When** `GET /api/v1/reports/attendance` is called
**Then** the response is 401 Unauthorized

---

### Story 7.2: Attendance Report — Pagination & Performance

As a Manager / HR Admin,
I want paginated attendance reports that complete within performance targets for large employee sets,
So that large result sets are navigable without degrading response times.

**Acceptance Criteria:**

**Given** a Manager or HR Admin requesting a report spanning many employees
**When** `GET /api/v1/reports/attendance` is called
**Then** results are paginated by `employeeId` cursor — not offset-based
**And** the default `pageSize` is 20 employees per page
**And** `nextCursor` is returned in the response body when additional pages exist

**Given** a Manager or HR Admin
**When** `GET /api/v1/reports/attendance?cursor={nextCursor}` is called with a valid cursor
**Then** the next page of results is returned starting from the cursor position
**And** results are stable across page fetches (cursor prevents drift from concurrent writes)

**Given** the report endpoint under load
**When** called for a 31-day range across 100 employees
**Then** the response completes in under 5 seconds (NFR-P2)
**And** all schedule, time log, approval, and holiday data is bulk-fetched in set-based queries before the computation loop — no query-inside-loop pattern
**And** `AsNoTracking()` is used on all read queries

**Given** the integration test suite in Integration.Tests
**When** Journey J8 (role-scoped attendance report) runs
**Then** an Employee token returns only the employee's own data and 403 for any other employee ID
**And** a Manager token returns only direct reports and 403 for employees outside scope
**And** an HR Admin token returns all employees without restriction
**And** all three role assertions pass with exact scope enforcement (NFR-T3)

---

## Epic 8: Feature Flag Administration

HR Admin can view all system feature flags and their current values, toggle the `HolidayPayWithinScheduledHours` flag to suppress or restore holiday pay within scheduled hours, and access an immutable audit log of all flag changes. DI switches from `HardcodedFeatureFlagProvider` to `DbFeatureFlagProvider`.

### Story 8.1: HR Admin Views & Toggles Feature Flags

As an HR Admin,
I want to view all system feature flags and their current values and toggle the `HolidayPayWithinScheduledHours` flag,
So that I can control whether holiday pay within scheduled hours is applied during attendance computation.

**Acceptance Criteria:**

**Given** an HR Admin with a valid JWT
**When** `GET /api/v1/config/feature-flags` is called
**Then** all feature flags and their current boolean values are returned with 200 OK (FR47)

**Given** an HR Admin with a valid JWT
**When** `PUT /api/v1/config/feature-flags/HolidayPayWithinScheduledHours` is called with `{ "enabled": false }`
**Then** the flag is updated to `false` and 200 OK is returned (FR48)
**And** subsequent computation calls suppress `Regular Holiday Paid Hours` and `Special Holiday Paid Hours` and their night differential variants
**And** OT classification types are unaffected by the flag state

**Given** an HR Admin
**When** `PUT /api/v1/config/feature-flags/HolidayPayWithinScheduledHours` is called with `{ "enabled": true }`
**Then** the flag is restored to `true` and holiday pay within scheduled hours resumes

**Given** the database is unavailable when a feature flag is evaluated
**When** `DbFeatureFlagProvider` attempts to read from the `FeatureFlag` table
**Then** the exception is caught and the default value of `true` is returned (fail-open)
**And** the API continues to function without returning an error to the caller

**Given** a Manager or Employee role JWT
**When** `GET /api/v1/config/feature-flags` is called
**Then** the response is 403 Forbidden (HR Admin only)

**Given** a Manager or Employee role JWT
**When** any feature flag write endpoint is called
**Then** the response is 403 Forbidden

---

### Story 8.2: Feature Flag Audit Log & DbFeatureFlagProvider Switchover

As the system / HR Admin,
I want all feature flag changes recorded in an immutable audit log within the same transaction, and DI switched to read live flag values from the database,
So that every flag change is fully traceable and the computation engine reflects the current flag state at all times.

**Acceptance Criteria:**

**Given** a successful feature flag toggle
**When** the toggle transaction completes
**Then** an immutable audit record is written in the same database transaction containing actor `sub` claim, timestamp, old value, and new value (FR49, NFR-R2)

**Given** an audit record is written
**When** it is queried
**Then** it cannot be modified or deleted — the record is append-only

**Given** `DbFeatureFlagProvider` is registered in DI replacing `HardcodedFeatureFlagProvider`
**When** any feature flag is evaluated
**Then** the value is read from the `FeatureFlag` table in the database
**And** the seeded initial values match `HardcodedFeatureFlagProvider` defaults (all flags `true`) so existing computation behavior is preserved at switchover

**Given** the integration test suite in Integration.Tests
**When** Journey J6 (HR onboarding + feature flag toggle) runs
**Then** HR Admin creates an employee, configures the holiday calendar, and toggles `HolidayPayWithinScheduledHours`
**And** the toggled flag state is reflected in subsequent computation results
**And** the audit log entry contains the correct actor, old value, and new value
**And** the journey passes with exact expected assertions (NFR-T3)
