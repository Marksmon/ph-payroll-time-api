# Story 1.3: API Versioning & RFC 7807 Problem Details

Status: review

## Story

As a developer / API consumer,
I want all routes versioned under `/api/v1/` and all error responses in RFC 7807 format,
So that breaking changes can be versioned independently and error handling is consistent for all consumers.

## Acceptance Criteria

1. **Given** any API endpoint **When** the route is inspected **Then** it is accessible under the `/api/v1/` path prefix (FR52) **And** `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer` are the versioning packages used

2. **Given** an unhandled exception occurs in any handler **When** `ExceptionHandlerMiddleware` processes it **Then** the response has `Content-Type: application/problem+json` **And** the body contains `type`, `title`, `status`, and `detail` per RFC 7807 (FR53) **And** each error category has a distinct `type` URI defined in the `ProblemTypes` static class

3. **Given** a request for a non-existent route **When** the router processes it **Then** the response is 404 with RFC 7807 Problem Details

4. **Given** a request with invalid model input **When** model validation fails **Then** the response is 400 with RFC 7807 Problem Details including field-level error details

5. **Given** Serilog is configured **When** the application starts **Then** structured logs are written to both console and rolling file sinks **And** each HTTP request is logged via `SerilogRequestLogging` middleware in the correct pipeline position

6. **Given** a domain exception is thrown in a handler **When** the global exception handler processes it **Then** `EntityNotFoundException` → 404, `DomainValidationException` → 400, `ScheduleOverlapException` → 409 with `ProblemTypes.ConflictOverlappingSchedule`, `StaleApprovalException` → 409 with `ProblemTypes.ConflictStaleApproval`, `ComputationInvariantException` → 422

## Tasks / Subtasks

- [x] **Task 1: Configure API versioning** (AC: 1)
  - [x] Update `Program.cs`: replace `builder.Services.AddControllers()` call to chain `.AddApiVersioning(...)` and `.AddApiExplorer(...)` configuration
  - [x] Set default version to `1.0`, `AssumeDefaultVersionWhenUnspecified = true`, `ReportApiVersions = true`
  - [x] Use route-segment versioning: `ApiVersionReader = new UrlSegmentApiVersionReader()`
  - [x] Configure `ApiExplorer` with `GroupNameFormat = "'v'VVV"` and `SubstituteApiVersionInUrl = true`
  - [x] Decorate `AuthController` and `PingController` with `[ApiVersion("1.0")]` attribute
  - [x] Update routes on `AuthController` to `[Route("api/v{version:apiVersion}/auth")]`
  - [x] Update routes on `PingController` to `[Route("api/v{version:apiVersion}/ping")]`

- [x] **Task 2: Create GlobalExceptionHandler** (AC: 2, 6)
  - [x] Create `src/PhPayrollTimeApi.Api/Infrastructure/GlobalExceptionHandler.cs`
  - [x] Implement `IExceptionHandler` interface (ASP.NET Core 8 built-in)
  - [x] Map exception types to RFC 7807 `ProblemDetails`:
    - `EntityNotFoundException` → 404, `type = ProblemTypes.NotFound`
    - `DomainValidationException` → 400, `type = ProblemTypes.Validation`
    - `ScheduleOverlapException` → 409, `type = ProblemTypes.ConflictOverlappingSchedule`
    - `StaleApprovalException` → 409, `type = ProblemTypes.ConflictStaleApproval`
    - `ComputationInvariantException` → 422, `type = ProblemTypes.ComputationInvariant`, include `Violations` list in extensions
    - All others → 500, `type = ProblemTypes.InternalError` (no exception detail in response)
  - [x] Register in `Program.cs`: `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()`
  - [x] Replace `builder.Services.AddProblemDetails()` registration — keep it, it's needed alongside `AddExceptionHandler`

- [x] **Task 3: Configure RFC 7807 for model validation errors** (AC: 4)
  - [x] In `Program.cs`, configure `ApiBehaviorOptions` to return RFC 7807 for model validation:
    ```csharp
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Type = ProblemTypes.Validation,
                Status = StatusCodes.Status400BadRequest
            };
            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });
    ```

- [x] **Task 4: Configure 404 for unknown routes** (AC: 3)
  - [x] In `Program.cs`, add fallback route that returns RFC 7807 404:
    ```csharp
    app.MapFallback(() => Results.Problem(
        type: ProblemTypes.NotFound,
        title: "Not Found",
        statusCode: 404,
        detail: "The requested endpoint does not exist."));
    ```

- [x] **Task 5: Write exception handler tests** (AC: 2, 3, 4, 6)
  - [x] Create `tests/PhPayrollTimeApi.Integration.Tests/Infrastructure/ExceptionHandlerTests.cs`
  - [x] Add a test-only minimal controller `ErrorProbeController` (registered only in Test environment) that throws specific domain exceptions on demand
  - [x] Tests:
    - `EntityNotFoundException_Returns404WithProblemDetails`
    - `DomainValidationException_Returns400WithProblemDetails`
    - `ScheduleOverlapException_Returns409WithOverlapType`
    - `UnhandledException_Returns500WithoutLeakingDetails`
    - `UnknownRoute_Returns404WithProblemDetails`
    - `InvalidModelInput_Returns400WithFieldErrors`

- [x] **Task 6: Verify Serilog is functional** (AC: 5)
  - [x] Confirm `logs/` directory is created on startup (Serilog file sink from Story 1.1)
  - [x] No changes needed — Serilog was fully configured in Story 1.1 (`Program.cs` + `appsettings.json`)
  - [x] Verify `SerilogRequestLogging` is at position 3 in pipeline (already done in 1.1)

## Dev Notes

### What Story 1.1 & 1.2 Built (preserve all)

`Program.cs` already has these — DO NOT regress:
- Serilog host builder, `UseSerilogRequestLogging()` at position 3
- `AddProblemDetails()` — KEEP, required alongside `AddExceptionHandler`
- `AddAuthentication(...).AddJwtBearer(...)` with RS256 config from Story 1.2
- Mandatory middleware pipeline positions 1–8

### API Versioning Configuration in Program.cs

Replace:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(opt => { ... });
```

With:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

The `AddApiVersioning` and `AddApiExplorer` are extension methods from `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` — already in `Api.csproj`.

### Controller Version Attribute Pattern

All controllers in this project use `[ApiVersion("1.0")]` and versioned route templates:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase { ... }
```

Update `AuthController` and `PingController` from Story 1.2 with these attributes.

### GlobalExceptionHandler

```csharp
// src/PhPayrollTimeApi.Api/Infrastructure/GlobalExceptionHandler.cs
using Microsoft.AspNetCore.Diagnostics;
using PhPayrollTimeApi.Api.Constants;
using PhPayrollTimeApi.Domain.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, type, title, detail, extensions) = exception switch
        {
            EntityNotFoundException ex => (404, ProblemTypes.NotFound, "Not Found", ex.Message, (IDictionary<string, object?>?)null),
            DomainValidationException ex => (400, ProblemTypes.Validation, "Validation Error", ex.Message, null),
            ScheduleOverlapException ex => (409, ProblemTypes.ConflictOverlappingSchedule, "Schedule Conflict", ex.Message, null),
            StaleApprovalException ex => (409, ProblemTypes.ConflictStaleApproval, "Stale Approval", ex.Message, null),
            ComputationInvariantException ex => (422, ProblemTypes.ComputationInvariant, "Computation Invariant Violated", ex.Message,
                new Dictionary<string, object?> { ["violations"] = ex.Violations }),
            _ => (500, ProblemTypes.InternalError, "An unexpected error occurred", "Please try again later.", null)
        };

        if (status == 500)
            _logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail
        };

        if (extensions is not null)
            foreach (var (key, value) in extensions)
                problem.Extensions[key] = value;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
```

Register before `AddProblemDetails`:
```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
```

### ProblemTypes Constants (already exist from Story 1.1)

`src/PhPayrollTimeApi.Api/Constants/ProblemTypes.cs` has all the type URIs. Use them — do not hardcode strings.

### Model Validation RFC 7807

`ValidationProblemDetails` extends `ProblemDetails` and includes `Errors` dictionary for field-level validation errors. The `ApiBehaviorOptions.InvalidModelStateResponseFactory` override in Task 3 ensures model binding errors return `application/problem+json` instead of the default ASP.NET Core format.

### Test Infrastructure: ErrorProbeController

To test exception handling without coupling to real business endpoints (which don't exist yet), add a test-only controller registered only in the `Test` environment:

```csharp
// tests/PhPayrollTimeApi.Integration.Tests/Infrastructure/ErrorProbeController.cs
// Or add to ApiTestFixture.ConfigureWebHost as an in-memory controller
```

The simplest approach is to add an internal controller in the API project guarded by environment check, similar to the AuthController pattern in Story 1.2.

Alternatively, use WebApplicationFactory's `WithWebHostBuilder` to add a minimal endpoint in the test:
```csharp
var client = _fixture.WithWebHostBuilder(b =>
    b.ConfigureServices(s => s.AddSingleton<IExceptionHandlerTestController, ...>()))
    .CreateClient();
```

For this story, use a `TestEndpoints` controller registered only in `Test` environment — add to `ApiTestFixture.ConfigureWebHost` using `services.AddControllers()` re-configuration or use minimal API test endpoints added via `app.Map(...)` in a test-specific startup.

The simplest working approach: add `ErrorProbeController` to the Integration.Tests project and register it via `AddApplicationPart`:
```csharp
// In ApiTestFixture.ConfigureWebHost:
services.AddControllers()
    .AddApplicationPart(typeof(ErrorProbeController).Assembly);
```

### File Modification Summary

| File | Action |
|---|---|
| `src/PhPayrollTimeApi.Api/Program.cs` | UPDATE: add versioning config, AddExceptionHandler, ApiBehaviorOptions, MapFallback |
| `src/PhPayrollTimeApi.Api/Infrastructure/GlobalExceptionHandler.cs` | CREATE |
| `src/PhPayrollTimeApi.Api/Controllers/AuthController.cs` | UPDATE: add `[ApiVersion("1.0")]` + versioned route |
| `src/PhPayrollTimeApi.Api/Controllers/PingController.cs` | UPDATE: add `[ApiVersion("1.0")]` + versioned route |
| `tests/PhPayrollTimeApi.Integration.Tests/Infrastructure/ExceptionHandlerTests.cs` | CREATE |
| `tests/PhPayrollTimeApi.Integration.Tests/Infrastructure/ErrorProbeController.cs` | CREATE |
| `tests/PhPayrollTimeApi.Integration.Tests/Fixtures/ApiTestFixture.cs` | UPDATE: register ErrorProbeController assembly |

### NuGet Packages

All already in `Api.csproj` from Story 1.1:
- `Asp.Versioning.Mvc 8.1.0`
- `Asp.Versioning.Mvc.ApiExplorer 8.1.0`

No new packages needed.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `ErrorProbeController` placed in Integration.Tests project and registered via `AddApplicationPart`
- `[Required]` attribute on `ModelValidationRequest` requires `System.ComponentModel.DataAnnotations` — implicit via framework

### Completion Notes List

- Program.cs updated: `AddApiVersioning`/`AddApiExplorer`, `GlobalExceptionHandler`, `ApiBehaviorOptions` for RFC 7807 model validation, `MapFallback` for 404
- `GlobalExceptionHandler`: maps all 5 domain exceptions + catch-all 500 with no detail leak
- AuthController + PingController updated with `[ApiVersion("1.0")]` and versioned route templates
- `ErrorProbeController` in Integration.Tests for testing all exception paths
- `ExceptionHandlerTests`: 8 tests covering all AC scenarios
- `ApiTestFixture` updated with `AddApplicationPart` to expose test controllers

### File List

- src/PhPayrollTimeApi.Api/Program.cs (updated)
- src/PhPayrollTimeApi.Api/Infrastructure/GlobalExceptionHandler.cs (new)
- src/PhPayrollTimeApi.Api/Controllers/AuthController.cs (updated)
- src/PhPayrollTimeApi.Api/Controllers/PingController.cs (updated)
- tests/PhPayrollTimeApi.Integration.Tests/Fixtures/ApiTestFixture.cs (updated)
- tests/PhPayrollTimeApi.Integration.Tests/Infrastructure/ErrorProbeController.cs (new)
- tests/PhPayrollTimeApi.Integration.Tests/Infrastructure/ExceptionHandlerTests.cs (new)
