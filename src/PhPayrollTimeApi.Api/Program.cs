using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PhPayrollTimeApi.Api.Constants;
using PhPayrollTimeApi.Api.Extensions;
using PhPayrollTimeApi.Api.Infrastructure;
using PhPayrollTimeApi.Api.Keys;
using PhPayrollTimeApi.Api.Middleware;
using PhPayrollTimeApi.Api.Services;
using PhPayrollTimeApi.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog structured logging
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

// MVC + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// API versioning — route segment /api/v{version}/
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

// RFC 7807 Problem Details — exception handler + model validation
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Model validation → RFC 7807
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Type = ProblemTypes.Validation,
            Status = StatusCodes.Status400BadRequest
        };
        return new ContentResult
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentType = "application/problem+json",
            Content = System.Text.Json.JsonSerializer.Serialize(problem,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                })
        };
    };
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PH Payroll Time API",
        Version = "v1",
        Description = "Philippine DOLE RA 6727 overtime rules REST API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "RS256-signed JWT. Obtain via POST /api/v1/auth/token (Development only)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.UseInlineDefinitionsForEnums();
});

// Infrastructure (DbContext + 5 domain interface implementations)
builder.Services.AddInfrastructure(builder.Configuration);

// Application CQRS handler registrations
builder.Services.AddApplicationServices();

// ── Key management: generate RSA key pair if missing (Dev only) ──
if (builder.Environment.IsDevelopment())
{
    var pubPath = builder.Configuration["Jwt:PublicKeyPath"]!;
    var privPath = builder.Configuration["Jwt:PrivateKeyPath"]!;
    KeyManagement.EnsureKeysExist(pubPath, privPath);

    builder.Services.AddScoped<ITestTokenService, RsaTestTokenService>();
}

// ── JWT RS256 Bearer authentication ──
var publicKeyPem = File.ReadAllText(builder.Configuration["Jwt:PublicKeyPath"]!);
var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);
var rsaKey = new RsaSecurityKey(rsa);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // preserve "sub" and "role" verbatim (set in Story 1.2)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKey,
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }   // NFR-S2: reject alg:none + HS256
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = ProblemTypes.Unauthorized,
                    title = "Unauthorized",
                    status = 401,
                    detail = "A valid RS256 Bearer token is required."
                }, options: null, contentType: "application/problem+json");
            }
        };
    });
builder.Services.AddAuthorization();

// Rate limiting — keyed by sub claim (NFR-S5: standard 300/min, bulk 20/min)
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

    options.AddPolicy("bulk", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst("sub")?.Value
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(60)
            }));
});

// Idempotency cache (Story 1.5)
builder.Services.AddMemoryCache();

var app = builder.Build();

// ── MANDATORY MIDDLEWARE PIPELINE ORDER (NFR-S5: UseAuthentication MUST precede UseRateLimiter) ──
app.UseExceptionHandler();                       // 1. catches all exceptions → RFC 7807
app.UseHttpsRedirection();                       // 2.
app.UseSerilogRequestLogging();                  // 3.
app.UseAuthentication();                         // 4. JWT validation (RS256 configured Story 1.2)
app.UseAuthorization();                          // 5.
app.UseRateLimiter();                            // 6. keyed by sub claim
app.UseMiddleware<IdempotencyMiddleware>();       // 7. SHA256 cache (implemented Story 1.5)
app.MapControllers();                            // 8.

// 404 fallback for unknown routes — RFC 7807
app.MapFallback(() => Results.Problem(
    type: ProblemTypes.NotFound,
    title: "Not Found",
    statusCode: 404,
    detail: "The requested endpoint does not exist."));

// Seed demo data in Development only (non-fatal if DB unavailable)
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhPayrollTimeApi.Infrastructure.Persistence.AppDbContext>();
        await PhPayrollTimeApi.Infrastructure.Persistence.DataSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "DataSeeder skipped — database unavailable");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PH Payroll Time API v1");
        options.RoutePrefix = "swagger";
    });
}

app.Run();

// Allow WebApplicationFactory<Program> in integration tests
public partial class Program { }
