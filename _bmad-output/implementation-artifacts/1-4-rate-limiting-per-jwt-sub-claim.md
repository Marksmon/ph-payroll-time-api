# Story 1.4: Rate Limiting per JWT Sub Claim

Status: review

## Story

As a system operator,
I want all authenticated API traffic rate-limited per `sub` claim with separate standard and bulk policies,
So that no single user can exhaust API capacity regardless of IP address (NFR-S5).

## Acceptance Criteria

1. **Given** an authenticated user exceeds 300 requests within 60 seconds on standard endpoints **When** the 301st request arrives **Then** the response is 429 Too Many Requests with RFC 7807 Problem Details and a `Retry-After` header

2. **Given** an authenticated user exceeds 20 requests within 60 seconds on bulk endpoints **When** the 21st request arrives **Then** the response is 429 with RFC 7807 Problem Details and `Retry-After`

3. **Given** two requests from the same `sub` claim arrive from different IP addresses **When** both are processed **Then** they share the same rate limit counter (keyed by `sub`, not IP)

4. **Given** the `Program.cs` middleware pipeline **When** middleware order is verified **Then** `UseAuthentication` precedes `UseRateLimiter` **And** full mandatory order is enforced

## Tasks / Subtasks

- [x] **Task 1: Configure rate limiter policies in Program.cs** (AC: 1, 2, 3)
  - [x] Replace placeholder `AddRateLimiter(opt => opt.RejectionStatusCode = 429)` with full config
  - [x] Define `standard` policy: fixed window, 300 req/60s, keyed by `sub` claim
  - [x] Define `bulk` policy: fixed window, 20 req/60s, keyed by `sub` claim
  - [x] Key resolver: `context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon"`
  - [x] On rejection: return RFC 7807 `application/problem+json` 429 with `Retry-After` header

- [x] **Task 2: Middleware order assertion test** (AC: 4)
  - [x] Create `tests/PhPayrollTimeApi.Domain.Tests/MiddlewarePipelineOrderTests.cs`
  - [x] Verify via reflection/string inspection that Program.cs contains `UseAuthentication` before `UseRateLimiter`

- [x] **Task 3: Rate limit integration test** (AC: 1, 3)
  - [x] Create `tests/PhPayrollTimeApi.Integration.Tests/RateLimiting/RateLimitTests.cs`
  - [x] Test: same sub exceeds limit → 429 with `Retry-After` header and `application/problem+json`

## Dev Notes

### Rate Limiter Config (replaces placeholder in Program.cs)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = ProblemTypes.RateLimitExceeded,
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. See Retry-After header."
        }, ct);
    };

    options.AddFixedWindowLimiter("standard", opt =>
    {
        opt.PermitLimit = 300;
        opt.Window = TimeSpan.FromSeconds(60);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("bulk", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromSeconds(60);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});
```

Key resolver is set per-policy using `keySelector` in the partition limiter. Use `RateLimiterOptions.GlobalLimiter` or per-policy partition. The `sub` claim is preserved verbatim from Story 1.2 (`MapInboundClaims = false`).

For per-user partitioning, use `CreatePartitioner`:
```csharp
options.AddPolicy("standard", httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.User.FindFirst("sub")?.Value
                      ?? httpContext.Connection.RemoteIpAddress?.ToString()
                      ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromSeconds(60)
        }));
```

Use `AddPolicy` (not `AddFixedWindowLimiter`) to support per-user partition keys.

### NuGet

`System.Threading.RateLimiting` is built into .NET 8 SDK. `Microsoft.AspNetCore.RateLimiting` is included in ASP.NET Core 8 framework — no new packages needed.

### Required using

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
```

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- Program.cs: replaced placeholder rate limiter with `AddPolicy("standard", ...)` + `AddPolicy("bulk", ...)` both partitioned by `sub` claim
- `OnRejected` returns RFC 7807 `application/problem+json` with `Retry-After` header
- `MiddlewarePipelineOrderTests.cs` verifies `UseAuthentication` precedes `UseRateLimiter` via source file inspection

### File List

- src/PhPayrollTimeApi.Api/Program.cs (updated)
- tests/PhPayrollTimeApi.Domain.Tests/MiddlewarePipelineOrderTests.cs (new)
- tests/PhPayrollTimeApi.Integration.Tests/RateLimiting/RateLimitTests.cs (new)
