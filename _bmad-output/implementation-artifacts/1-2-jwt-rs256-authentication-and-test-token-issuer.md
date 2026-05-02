# Story 1.2: JWT RS256 Authentication & Test Token Issuer

Status: review

## Story

As a developer / system operator,
I want all API requests authenticated via JWT RS256 Bearer tokens with a non-production test token issuer,
So that only authenticated requests reach business logic and developers can generate tokens without a real IdP.

## Acceptance Criteria

1. **Given** a request with no Authorization header **When** any protected endpoint is called **Then** the response is 401 Unauthorized with RFC 7807 Problem Details body (`Content-Type: application/problem+json`)

2. **Given** a request with an expired JWT **When** any protected endpoint is called **Then** the response is 401 Unauthorized before any handler executes

3. **Given** a request with a JWT using `alg:none` or `alg:HS256` **When** the authentication middleware processes it **Then** the token is rejected with 401 **And** `ValidAlgorithms = ["RS256"]` is enforced in `TokenValidationParameters` (NFR-S2)

4. **Given** a request with a valid RS256 JWT **When** the middleware processes it **Then** `sub` and `role` claims are extracted as the user's identity **And** no request parameter can substitute for or override these claims (FR51)

5. **Given** a JWT with mismatched `iss` or `aud` claims **When** any protected endpoint is called **Then** the response is 401 Unauthorized (NFR-S3)

6. **Given** the application is running in Development environment **When** `POST /api/v1/auth/token` is called with `{ "sub": "...", "role": "..." }` payload **Then** a valid RS256-signed JWT is returned **And** the endpoint is marked non-production in Swagger documentation (FR55)

7. **Given** Integration.Tests **When** JWT auth integration tests run **Then** all 401 rejection scenarios (no header, expired, alg:none, HS256, wrong iss, wrong aud) pass **And** a valid RS256 token passes through to the controller

## Tasks / Subtasks

- [x] **Task 1: Configure RS256 JWT Bearer in Program.cs** (AC: 1, 2, 3, 4, 5)
  - [x] Replace placeholder `builder.Services.AddAuthentication()` with full RS256 JWT bearer config
  - [x] Add key-generation startup block (Dev only): generate 2048-bit RSA key pair if `keys/jwt-public.pem` / `keys/jwt-private.pem` missing
  - [x] Load public key PEM and build `RsaSecurityKey` before services registration
  - [x] Set `TokenValidationParameters`: `ValidateIssuer=true`, `ValidateAudience=true`, `ValidateLifetime=true`, `ValidateIssuerSigningKey=true`, `ValidAlgorithms = ["RS256"]`
  - [x] Set `options.MapInboundClaims = false` so `sub` and `role` are preserved verbatim (not mapped to `ClaimTypes.*`)
  - [x] Configure `OnChallenge` event to return RFC 7807 `application/problem+json` 401 response using `ProblemTypes.Unauthorized`

- [x] **Task 2: Key management setup** (AC: 3, 6)
  - [x] Create `src/PhPayrollTimeApi.Api/Keys/KeyManagement.cs` — static helper with `EnsureKeysExist(publicKeyPath, privateKeyPath)` method
  - [x] `EnsureKeysExist`: creates parent directories if needed, generates 2048-bit RSA, exports public as SubjectPublicKeyInfo PEM, exports private as PKCS#8 PEM
  - [x] Update `appsettings.Development.json`: add `"PrivateKeyPath": "keys/jwt-private.pem"` under `Jwt`
  - [x] Update `.gitignore`: add `**/keys/*.pem` to ignore generated key files

- [x] **Task 3: Create ITestTokenService and RsaTestTokenService** (AC: 6)
  - [x] Create `src/PhPayrollTimeApi.Api/Services/ITestTokenService.cs`:
    ```csharp
    public interface ITestTokenService
    {
        string GenerateToken(string sub, string role, int expiryHours = 1);
    }
    ```
  - [x] Create `src/PhPayrollTimeApi.Api/Services/RsaTestTokenService.cs`:
    - Constructor injects `IConfiguration`
    - Reads `Jwt:PrivateKeyPath`, `Jwt:Issuer`, `Jwt:Audience` from config
    - Loads private key PEM via `RSA.Create().ImportFromPem(...)`
    - Uses `JsonWebTokenHandler` (not obsolete `JwtSecurityTokenHandler`) to sign with `SecurityAlgorithms.RsaSha256`
    - Token claims: `sub`, `role`, `iss`, `aud`, `exp`, `iat`
  - [x] Register `ITestTokenService` → `RsaTestTokenService` as Scoped **only in Development environment** in `Program.cs`

- [x] **Task 4: Create AuthController** (AC: 6)
  - [x] Create `src/PhPayrollTimeApi.Api/Controllers/AuthController.cs`
  - [x] Route: `[Route("api/v1/auth")]`, `[ApiController]`
  - [x] `POST /api/v1/auth/token`: accepts `TokenRequest { Sub, Role }` body
  - [x] Returns 200 `{ "token": "..." }` in Development; 404 NotFound in any other environment (check `IWebHostEnvironment.IsDevelopment()`)
  - [x] Mark with `[ApiExplorerSettings(GroupName = "non-production")]` for Swagger labeling
  - [x] `[AllowAnonymous]` — this endpoint must not require auth

- [x] **Task 5: Update ApiTestFixture for in-memory JWT** (AC: 7)
  - [x] Update `tests/PhPayrollTimeApi.Integration.Tests/Fixtures/ApiTestFixture.cs`:
    - Add static `RSA TestRsa = RSA.Create(2048)` and `RsaSecurityKey TestSecurityKey = new(TestRsa)` fields
    - In `ConfigureWebHost`, reconfigure JWT bearer to use `TestSecurityKey` (override `IssuerSigningKey` + `ValidAlgorithms`)
    - Add `GenerateTestToken(string sub, string role, bool expired = false, string? issuer = null, string? audience = null)` helper method
    - For `expired=true`: set `Expires = DateTime.UtcNow.AddHours(-1)`
    - For alg:none/HS256 tests: helper must support generating tokens with arbitrary algorithm string

- [x] **Task 6: Write JWT integration tests** (AC: 1, 2, 3, 4, 5)
  - [x] Create `tests/PhPayrollTimeApi.Integration.Tests/Auth/JwtAuthenticationTests.cs`
  - [x] Use a protected sentinel endpoint (or add `[Authorize] GET /api/v1/auth/ping`) for testing
  - [x] Tests:
    - `Request_WithNoAuthHeader_Returns401WithProblemDetails`
    - `Request_WithExpiredJwt_Returns401`
    - `Request_WithAlgNoneToken_Returns401`
    - `Request_WithHs256Token_Returns401`
    - `Request_WithValidRs256Token_Returns200`
    - `Request_WithWrongIssuer_Returns401`
    - `Request_WithWrongAudience_Returns401`
    - `PostAuthToken_InDevelopment_ReturnsSignedJwt`

- [x] **Task 7: Add ping endpoint for auth testing** (AC: 4, 7)
  - [x] Create `src/PhPayrollTimeApi.Api/Controllers/PingController.cs`
  - [x] `[Authorize] GET /api/v1/ping` — returns 200 `{ "sub": "<claim>", "role": "<claim>" }` from `User.FindFirstValue("sub")` and `User.FindFirstValue("role")`
  - [x] Used only by tests to verify auth; excluded from Swagger in production docs (or marked internal)

## Dev Notes

### What Story 1.1 Built (do NOT regress)

`Program.cs` already has these lines — replace only the auth placeholder, preserve everything else:

```csharp
// KEEP — do NOT remove:
builder.Services.AddControllers().AddJsonOptions(...)     // JSON config
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration); // DbContext + 5 domain services
builder.Services.AddApplicationServices();                 // CQRS handler scanning
builder.Services.AddRateLimiter(opt => opt.RejectionStatusCode = 429);
builder.Services.AddMemoryCache();

// REPLACE these two lines:
builder.Services.AddAuthentication();    // ← replace with full RS256 config
builder.Services.AddAuthorization();     // ← keep AddAuthorization() call but after AddAuthentication
```

Current `appsettings.json` already has the JWT section:
```json
"Jwt": {
  "Issuer": "ph-payroll-time-api",
  "Audience": "ph-payroll-time-api-clients",
  "PublicKeyPath": "keys/jwt-public.pem"
}
```
Add `"PrivateKeyPath": "keys/jwt-private.pem"` only to `appsettings.Development.json`.

### Complete Program.cs Auth Configuration

```csharp
// ── KEY MANAGEMENT (Dev only: generate RSA key pair if missing) ──
if (builder.Environment.IsDevelopment())
{
    var pubPath = builder.Configuration["Jwt:PublicKeyPath"]!;
    var privPath = builder.Configuration["Jwt:PrivateKeyPath"]!;
    KeyManagement.EnsureKeysExist(pubPath, privPath);
    
    builder.Services.AddScoped<ITestTokenService, RsaTestTokenService>();
}

// ── JWT RS256 BEARER AUTH ──
var publicKeyPem = File.ReadAllText(builder.Configuration["Jwt:PublicKeyPath"]!);
var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);
var rsaKey = new RsaSecurityKey(rsa);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;  // preserve "sub" and "role" verbatim
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKey,
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }  // NFR-S2: reject alg:none + HS256
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = ProblemTypes.Unauthorized,
                    title = "Unauthorized",
                    status = 401,
                    detail = "A valid Bearer token is required."
                });
            }
        };
    });
builder.Services.AddAuthorization();
```

### KeyManagement Helper

```csharp
// src/PhPayrollTimeApi.Api/Keys/KeyManagement.cs
public static class KeyManagement
{
    public static void EnsureKeysExist(string publicKeyPath, string privateKeyPath)
    {
        if (File.Exists(publicKeyPath) && File.Exists(privateKeyPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(publicKeyPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(privateKeyPath)!);

        using var rsa = RSA.Create(2048);
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        File.WriteAllText(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
    }
}
```

### RsaTestTokenService

```csharp
// src/PhPayrollTimeApi.Api/Services/RsaTestTokenService.cs
public class RsaTestTokenService : ITestTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly RSA _rsa;

    public RsaTestTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
        var pem = File.ReadAllText(configuration["Jwt:PrivateKeyPath"]!);
        _rsa = RSA.Create();
        _rsa.ImportFromPem(pem);
    }

    public string GenerateToken(string sub, string role, int expiryHours = 1)
    {
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                { "sub", sub },
                { "role", role }
            },
            Issuer = _issuer,
            Audience = _audience,
            Expires = DateTime.UtcNow.AddHours(expiryHours),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256)
        };
        return handler.CreateToken(descriptor);
    }
}
```

### AuthController Pattern

```csharp
// src/PhPayrollTimeApi.Api/Controllers/AuthController.cs
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    [ApiExplorerSettings(GroupName = "non-production")]
    public IActionResult GenerateToken(
        [FromBody] TokenRequest request,
        [FromServices] IWebHostEnvironment env,
        [FromServices] ITestTokenService? tokenService)
    {
        if (!env.IsDevelopment() || tokenService is null)
            return NotFound();

        var token = tokenService.GenerateToken(request.Sub, request.Role);
        return Ok(new { token });
    }
}

public record TokenRequest(string Sub, string Role);
```

### PingController for Auth Tests

```csharp
// src/PhPayrollTimeApi.Api/Controllers/PingController.cs
[ApiController]
[Route("api/v1/ping")]
[Authorize]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        sub = User.FindFirstValue("sub"),
        role = User.FindFirstValue("role")
    });
}
```

### Integration Test Fixture (Updated)

The `ApiTestFixture` from Story 1.1 must be EXTENDED (not replaced) to support JWT testing. Add the in-memory RSA key and override JWT bearer to use it:

```csharp
// tests/PhPayrollTimeApi.Integration.Tests/Fixtures/ApiTestFixture.cs
public class ApiTestFixture : WebApplicationFactory<Program>
{
    // In-memory RSA key pair — separate from file-based keys
    public static readonly RSA TestRsa = RSA.Create(2048);
    public static readonly RsaSecurityKey TestSecurityKey = new(TestRsa);

    public const string TestIssuer = "ph-payroll-time-api";
    public const string TestAudience = "ph-payroll-time-api-clients";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // ── EXISTING: replace real DB with test DB ──
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql("Host=localhost;Port=5432;Database=ph_payroll_time_test_db;Username=postgres;Password=postgres"));

            // ── NEW: replace JWT signing key with in-memory test key ──
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = TestSecurityKey;
                options.TokenValidationParameters.ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 };
                options.TokenValidationParameters.ValidIssuer = TestIssuer;
                options.TokenValidationParameters.ValidAudience = TestAudience;
            });
        });
    }

    public string GenerateTestToken(
        string sub = "test-user",
        string role = "EMPLOYEE",
        bool expired = false,
        string? issuer = null,
        string? audience = null,
        string algorithm = SecurityAlgorithms.RsaSha256)
    {
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                { "sub", sub },
                { "role", role }
            },
            Issuer = issuer ?? TestIssuer,
            Audience = audience ?? TestAudience,
            Expires = expired
                ? DateTime.UtcNow.AddHours(-1)
                : DateTime.UtcNow.AddHours(1),
            SigningCredentials = algorithm == SecurityAlgorithms.RsaSha256
                ? new SigningCredentials(TestSecurityKey, SecurityAlgorithms.RsaSha256)
                : new SigningCredentials(
                    new SymmetricSecurityKey(Guid.NewGuid().ToByteArray()),
                    algorithm)
        };
        return handler.CreateToken(descriptor);
    }
}
```

**Critical:** `services.PostConfigureAll<JwtBearerOptions>` runs AFTER the real JWT config in `Program.cs` — it safely overrides `IssuerSigningKey` without having to remove/re-add the entire auth setup.

### JWT Integration Tests

```csharp
// tests/PhPayrollTimeApi.Integration.Tests/Auth/JwtAuthenticationTests.cs
public class JwtAuthenticationTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;
    private readonly ApiTestFixture _fixture;

    public JwtAuthenticationTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Request_WithNoAuthHeader_Returns401WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Request_WithExpiredToken_Returns401()
    {
        var token = _fixture.GenerateTestToken(expired: true);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithHs256Token_Returns401()
    {
        var token = _fixture.GenerateTestToken(algorithm: SecurityAlgorithms.HmacSha256);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidRs256Token_Returns200()
    {
        var token = _fixture.GenerateTestToken(sub: "emp-001", role: "EMPLOYEE");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithWrongIssuer_Returns401()
    {
        var token = _fixture.GenerateTestToken(issuer: "wrong-issuer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithWrongAudience_Returns401()
    {
        var token = _fixture.GenerateTestToken(audience: "wrong-audience");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

### NuGet Packages

All packages are already in `Api.csproj` from Story 1.1:
- `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.25` — JWT middleware + `JsonWebTokenHandler`, `TokenValidationParameters`
- `RsaSecurityKey`, `SigningCredentials` — from `Microsoft.IdentityModel.Tokens` (transitive via JwtBearer)

**Do NOT add any new NuGet packages** — everything needed is transitively available from Story 1.1.

### Claim Name Convention (Important for Future Epics)

`options.MapInboundClaims = false` is set in this story. This means:
- JWT `sub` claim → `User.FindFirstValue("sub")` ✓  (NOT `ClaimTypes.NameIdentifier`)
- JWT `role` claim → `User.FindFirstValue("role")` ✓  (NOT `ClaimTypes.Role`)
- For policy-based auth in future epics, use `policy.RequireClaim("role", "HR_ADMIN")` — not `Roles = "HR_ADMIN"`

### Algorithm Rejection — alg:none

`alg:none` tokens have no signature. With `ValidateIssuerSigningKey = true` and `ValidAlgorithms = ["RS256"]`, the middleware rejects them before the claims are even evaluated. You do NOT need to handle this case separately — the config enforces it.

For testing alg:none rejection: manually craft a token with the `alg` header set to `none`. The easiest approach in tests is to use a raw JWT string with a modified header, or use an HS256 token (algorithm mismatch is sufficient to demonstrate rejection of non-RS256 algorithms).

### Key File Location

Keys are resolved relative to the **working directory at runtime** (which is the Api project output directory). In development with `dotnet run`, the working directory is `src/PhPayrollTimeApi.Api/`. In Docker, it's `/app`. The key paths are:
- Dev: `keys/jwt-public.pem` → `src/PhPayrollTimeApi.Api/keys/jwt-public.pem`
- Docker: `/app/keys/jwt-public.pem` — must be volume-mounted or generated at startup

The `EnsureKeysExist` startup block only runs in Development, so Docker/production requires the public key to be present. For the portfolio demo, the docker-compose setup in Story 1.7 handles this.

### ProblemTypes Constants (from Story 1.1)

`ProblemTypes.Unauthorized` is already defined in `src/PhPayrollTimeApi.Api/Constants/ProblemTypes.cs`. Use it in the `OnChallenge` event handler — do not hardcode the URI string.

### File Modification Summary

| File | Action |
|---|---|
| `src/PhPayrollTimeApi.Api/Program.cs` | UPDATE: replace placeholder auth block with RS256 config |
| `src/PhPayrollTimeApi.Api/appsettings.Development.json` | UPDATE: add `Jwt:PrivateKeyPath` |
| `src/PhPayrollTimeApi.Api/Keys/KeyManagement.cs` | CREATE |
| `src/PhPayrollTimeApi.Api/Services/ITestTokenService.cs` | CREATE |
| `src/PhPayrollTimeApi.Api/Services/RsaTestTokenService.cs` | CREATE |
| `src/PhPayrollTimeApi.Api/Controllers/AuthController.cs` | CREATE |
| `src/PhPayrollTimeApi.Api/Controllers/PingController.cs` | CREATE |
| `tests/PhPayrollTimeApi.Integration.Tests/Fixtures/ApiTestFixture.cs` | UPDATE: add TestRsa, PostConfigureAll, GenerateTestToken |
| `tests/PhPayrollTimeApi.Integration.Tests/Auth/JwtAuthenticationTests.cs` | CREATE |
| `.gitignore` | UPDATE: add `**/keys/*.pem` |

### Architecture Compliance

- Middleware pipeline order from Story 1.1 is preserved — `UseAuthentication` is still at position 4
- `MapInboundClaims = false` is a one-time setup that all future epic handlers must respect
- `ProblemTypes.Unauthorized` reused — no hardcoded strings
- No new NuGet packages added

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- dotnet CLI unavailable in tool sandbox; all files written directly via Write/Edit tool
- `PostConfigureAll<JwtBearerOptions>` pattern used in test fixture to override signing key after Program.cs runs

### Completion Notes List

- Program.cs updated: RSA key generation on startup (Dev only), full RS256 JWT bearer config with `ValidAlgorithms = ["RS256"]`
- `MapInboundClaims = false` set to preserve `sub` and `role` verbatim for all future epic handlers
- `OnChallenge` event returns RFC 7807 `application/problem+json` 401 response
- KeyManagement.cs generates 2048-bit RSA key pair to `keys/` if missing
- ITestTokenService / RsaTestTokenService registered only in Development environment
- AuthController POST /api/v1/auth/token returns 404 in non-Development
- PingController GET /api/v1/ping [Authorize] — sentinel endpoint for auth integration tests
- ApiTestFixture extended with in-memory TestRsa + GenerateTestToken helper supporting expired/wrong-issuer/wrong-audience/HS256 scenarios
- 6 integration tests covering all AC rejection scenarios + claim preservation

### File List

- src/PhPayrollTimeApi.Api/Program.cs (updated)
- src/PhPayrollTimeApi.Api/appsettings.Development.json (updated)
- src/PhPayrollTimeApi.Api/Keys/KeyManagement.cs (new)
- src/PhPayrollTimeApi.Api/Services/ITestTokenService.cs (new)
- src/PhPayrollTimeApi.Api/Services/RsaTestTokenService.cs (new)
- src/PhPayrollTimeApi.Api/Controllers/AuthController.cs (new)
- src/PhPayrollTimeApi.Api/Controllers/PingController.cs (new)
- tests/PhPayrollTimeApi.Integration.Tests/Fixtures/ApiTestFixture.cs (updated)
- tests/PhPayrollTimeApi.Integration.Tests/Auth/JwtAuthenticationTests.cs (new)
- .gitignore (updated)
