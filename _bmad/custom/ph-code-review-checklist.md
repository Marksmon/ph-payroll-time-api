# PH Payroll Time API — Code Review Checklist

Apply these project-specific checks on every code review in addition to general correctness:

## Security (High — any failure blocks merge)

- [ ] Every new controller action has `[Authorize]` or explicit `[AllowAnonymous]` — no unprotected endpoint
- [ ] Role restrictions exactly match story ACs (`[Authorize(Roles = "HR_ADMIN")]` vs `"MANAGER,HR_ADMIN"`)
- [ ] Employee self-service endpoints scope by `User.FindFirstValue("sub")` against `JwtSubjectClaim` — never trust a request-body or route ID for ownership
- [ ] No raw SQL or string-concatenated queries — EF Core parameterized queries only

## DateTimeOffset / Npgsql 8 (High — Npgsql 8 rejects non-UTC at runtime)

- [ ] No `DateTimeOffset` constructed with non-UTC offset (e.g., `TimeSpan.FromHours(8)` → reject)
- [ ] Timestamps use `DateTimeOffset.UtcNow`, not `DateTime.Now` or `DateTime.UtcNow`
- [ ] No `DateTime` type anywhere — only `DateTimeOffset`
- [ ] PHT times converted to UTC before storage: 8am PHT = 00:00 UTC, 10pm PHT = 14:00 UTC

## RFC 7807 / Problem Details (High)

- [ ] `WriteAsJsonAsync(...)` always passes `contentType: "application/problem+json"` — omitting it resets Content-Type to `application/json`
- [ ] Validation failure responses use `ContentResult` (not `ObjectResult` or `BadRequestObjectResult`) to preserve problem+json content type
- [ ] New exception types are registered in `GlobalExceptionHandler` with correct status code and `type` URI from `ProblemTypes`

## EF Core / Data Access (Medium)

- [ ] Read-only queries (GET endpoints) use `AsNoTracking()`
- [ ] No N+1 patterns — use `Include()` or batch queries instead of per-item DB calls
- [ ] New entity configs follow snake_case column naming (`HasColumnName("employee_number")`)
- [ ] New enum properties stored as string via `HasConversion<string>()`
- [ ] New `DateTimeOffset` properties covered by `ConfigureConventions` → `timestamptz` (global convention, no per-property `.HasColumnType()` needed unless overriding)

## CQRS Architecture (Medium)

- [ ] New handlers implement `ICommandHandler<TCommand>` or `IQueryHandler<TQuery, TResult>` — no mediator package, no base class
- [ ] Handlers live in `Application/Commands/` or `Application/Queries/` — not in Infrastructure or Api
- [ ] Controllers contain no business logic — only inject handler, call `HandleAsync`, map result to HTTP response
- [ ] `AddApplicationServices()` assembly scan picks up new handlers automatically (no manual registration needed)

## Domain Rules (High where applicable)

- [ ] Employee delete sets `IsActive = false` — never physical delete (FK integrity with TimeLogs)
- [ ] Schedule overlap check runs before any create/update of `ShiftSchedule` → 409 on conflict
- [ ] `BreakWindow` times fall within `ScheduleStart`–`ScheduleEnd` range

## Integration Tests (Medium — High for security ACs)

- [ ] Happy path test for each new endpoint (correct role + valid data → expected 2xx)
- [ ] 403 test for each endpoint with wrong role
- [ ] 400 test with missing/invalid required fields — asserts `Content-Type: application/problem+json`
- [ ] 404 test for non-existent entity where applicable
- [ ] 409 test where overlap/duplicate validation exists
- [ ] Tokens use `_fixture.GenerateTestToken(sub, role)` — no hardcoded JWTs
- [ ] Error response assertions check `Content-Type: application/problem+json`, not just status code
