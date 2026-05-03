# ph-payroll-time-api

**Philippine DOLE RA 6727 Payroll Time API** — a production-grade REST API for employee time tracking, shift scheduling, and overtime pay computation under Philippine labor law. Built with .NET 8, Clean Architecture, and CQRS.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791?logo=postgresql)
![License](https://img.shields.io/badge/license-MIT-blue)

---

## What It Does

Companies operating in the Philippines must compute employee overtime, night differential, and holiday pay according to **DOLE RA 6727** (Wage Rationalization Act). This API handles:

- **Employee management** — profiles, roles, and JWT identity (EMPLOYEE / MANAGER / HR_ADMIN)
- **Shift scheduling** — work windows, break periods, and weekly rest-day patterns
- **Time logging** — clock IN/OUT events from any source
- **Payroll computation** — overtime pay, night differential (10 PM–6 AM), regular holiday pay, special non-working day pay — all computed from actual time logs and the employee's assigned schedule
- **Approval workflows** — manager queues for holiday, rest-day, and OT approvals with bulk-commit support
- **Holiday calendar** — HR-managed national and special non-working days

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    API Layer                         │
│   Controllers · JWT Auth · Idempotency · Rate Limit  │
└───────────────────────┬─────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────┐
│               Application Layer (CQRS)               │
│   Commands · Queries · Handler Interfaces            │
└───────────────────────┬─────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────┐
│                  Domain Layer                        │
│   Entities · ComputationEngine · Enums · Interfaces  │
│   (zero external dependencies)                       │
└───────────────────────┬─────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────┐
│              Infrastructure Layer                    │
│   EF Core 8 · Npgsql · Repositories · DataSeeder    │
└─────────────────────────────────────────────────────┘
```

**Key design choices:**
- **CQRS without a mediator library** — controllers inject `ICommandHandler<T>` / `IQueryHandler<T,R>` directly; handlers are auto-registered via assembly scan
- **RFC 7807 Problem Details** — every error returns `application/problem+json` with a typed URI
- **RS256 JWT** — asymmetric key pair; `alg:none` and HS256 are rejected at the token validator
- **Idempotency middleware** — SHA-256 cache key prevents duplicate mutations on retry
- **Real integration tests** — `WebApplicationFactory` + real PostgreSQL 17; no mocking (past incident where mocked tests passed but a prod migration failed)

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 8, ASP.NET Core |
| ORM | EF Core 8, Npgsql 8 |
| Database | PostgreSQL 17 |
| Auth | JWT RS256 (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| API Docs | Swagger / OpenAPI (`Swashbuckle.AspNetCore` 6.9) |
| Versioning | `Asp.Versioning.Mvc` 8.1 |
| Logging | Serilog (console + rolling file) |
| Tests | xUnit, `WebApplicationFactory` |

---

## Quick Start (Docker)

```bash
git clone https://github.com/Marksmon/ph-payroll-time-api.git
cd ph-payroll-time-api
docker compose up
```

The API starts on **http://localhost:8080**. Swagger UI is at **http://localhost:8080/swagger**.

RSA keys are auto-generated on first run. The database is seeded with three demo users.

---

## Manual Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL 17 running locally on port 5432

### 1. Configure the connection string

Edit `src/PhPayrollTimeApi.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ph_payroll_time;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "PublicKeyPath": "keys/jwt-public.pem",
    "PrivateKeyPath": "keys/jwt-private.pem",
    "Issuer": "ph-payroll-time-api",
    "Audience": "ph-payroll-time-client"
  }
}
```

### 2. Apply migrations

```powershell
dotnet ef database update `
  --project src/PhPayrollTimeApi.Infrastructure `
  --startup-project src/PhPayrollTimeApi.Api
```

### 3. Run the API

```powershell
dotnet run --project src/PhPayrollTimeApi.Api
```

Swagger UI: **https://localhost:7001/swagger**

---

## Usage Examples

### Get a test token (Development only)

```bash
curl -X POST https://localhost:7001/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"sub": "mgr-001", "role": "MANAGER"}'
```

```json
{ "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..." }
```

Seeded identities: `emp-001` (EMPLOYEE), `mgr-001` (MANAGER), `hr-001` (HR_ADMIN).

---

### Create an employee

```bash
curl -X POST https://localhost:7001/api/v1/employees \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: create-emp-abc123" \
  -d '{
    "employeeNumber": "EMP-100",
    "fullName": "Juan dela Cruz",
    "role": "EMPLOYEE",
    "jwtSubjectClaim": "juan-001"
  }'
```

---

### Assign a shift schedule

```bash
curl -X POST https://localhost:7001/api/v1/employees/1/shift-schedules \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "scheduleStart": "2025-06-01T00:00:00Z",
    "scheduleEnd": "2025-06-30T23:59:59Z",
    "workStart": "2025-06-01T01:00:00Z",
    "workEnd": "2025-06-01T10:00:00Z"
  }'
```

> All timestamps are UTC. Philippine Standard Time (UTC+8) must be converted before sending.

---

### Compute overtime pay

```bash
curl https://localhost:7001/api/v1/schedules/1/computation \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "employeeId": 1,
  "regularHours": 8.0,
  "overtimeHours": 2.5,
  "nightDifferentialHours": 1.0,
  "holidayMultiplier": 1.0,
  "segments": [
    { "classification": "REGULAR", "hours": 8.0 },
    { "classification": "OVERTIME", "hours": 2.5 },
    { "classification": "NIGHT_DIFFERENTIAL", "hours": 1.0 }
  ]
}
```

---

### Bulk approve OT actions (Manager)

```bash
curl -X POST https://localhost:7001/api/v1/approvals/ot/commit \
  -H "Authorization: Bearer $MANAGER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "employeeId": 1, "scheduleId": 5 }'
```

---

## API Reference

| Resource | Method | Path | Role |
|----------|--------|------|------|
| Employees | `GET POST` | `/api/v1/employees` | MANAGER / HR_ADMIN |
| Employee | `GET PUT PATCH` | `/api/v1/employees/{id}` | MANAGER / HR_ADMIN |
| Shift Schedules | `GET POST` | `/api/v1/employees/{id}/shift-schedules` | Any |
| Work Patterns | `GET POST PUT` | `/api/v1/employees/{id}/work-schedule-patterns` | Any |
| Time Logs | `GET POST` | `/api/v1/employees/{id}/time-logs` | EMPLOYEE |
| Computation | `GET` | `/api/v1/schedules/{id}/computation` | Any |
| Holidays | `GET POST PUT DELETE` | `/api/v1/holidays` | HR_ADMIN |
| OT Queue | `GET` | `/api/v1/approvals/ot-queue` | MANAGER |
| Holiday Queue | `GET` | `/api/v1/approvals/holiday-queue` | MANAGER |
| Rest-Day Queue | `GET` | `/api/v1/approvals/rest-day-queue` | MANAGER |
| Stage OT | `POST` | `/api/v1/approvals/ot/stage` | MANAGER |
| Commit OT | `POST` | `/api/v1/approvals/ot/commit` | MANAGER |
| Bulk Approve Holidays | `POST` | `/api/v1/approvals/holidays/bulk-approve` | MANAGER |
| Bulk Approve Rest Days | `POST` | `/api/v1/approvals/rest-days/bulk-approve` | MANAGER |
| Auth Token (dev) | `POST` | `/api/v1/auth/token` | Dev only |
| Health | `GET` | `/api/v1/ping` | Public |

Full schema available at `/swagger` when running in Development.

---

## Running Tests

Requires PostgreSQL 17 running locally on port 5432.

```powershell
dotnet test ph-payroll-time-api.sln
```

The test suite covers:
- DOLE RA 6727 computation rules (unit)
- JWT RS256 authentication (integration)
- All CRUD endpoints with role-based access (integration)
- Idempotency middleware deduplication (integration)
- RFC 7807 error response format (integration)
- Rate limiting enforcement (integration)

---

## Project Structure

```
src/
  PhPayrollTimeApi.Domain/          # Entities, ComputationEngine, interfaces (no deps)
  PhPayrollTimeApi.Application/     # CQRS commands, queries, DTOs
  PhPayrollTimeApi.Infrastructure/  # EF Core, Npgsql, repositories, migrations
  PhPayrollTimeApi.Api/             # Controllers, middleware, JWT, Swagger

tests/
  PhPayrollTimeApi.Domain.Tests/    # Computation engine unit tests
  PhPayrollTimeApi.Application.Tests/
  PhPayrollTimeApi.Integration.Tests/ # WebApplicationFactory + real PostgreSQL
```

---

## License

MIT
