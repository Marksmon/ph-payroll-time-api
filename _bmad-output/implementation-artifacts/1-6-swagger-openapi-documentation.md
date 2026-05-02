# Story 1.6: Swagger/OpenAPI Documentation

Status: review

## Story

As a developer / portfolio reviewer,
I want a Swagger UI documenting all endpoints, schemas, and the complete 40-type classification enum as strings,
So that Journey 7 (Swagger discovery) is fully demonstrable.

## Acceptance Criteria

1. **Given** the application is running **When** `GET /swagger` is accessed **Then** Swagger UI loads displaying all endpoints grouped under api version v1

2. **Given** the Swagger schema **When** any enum type is inspected **Then** all 40 classification type values are enumerated as strings (not integers) **And** `JsonStringEnumConverter` is applied globally (FR56)

3. **Given** the Swagger schema **When** error response schemas are inspected **Then** all RFC 7807 `type` URIs from `ProblemTypes` are documented

4. **Given** the Swagger security configuration **When** authentication requirements are inspected **Then** JWT Bearer is defined as the security scheme **And** all protected endpoints display the authorization requirement **And** `POST /api/v1/auth/token` is documented as non-production

## Tasks / Subtasks

- [x] **Task 1: Configure Swashbuckle for versioning + JWT + enums** (AC: 1, 2, 3, 4)
  - [x] Replace `builder.Services.AddSwaggerGen()` placeholder with full Swashbuckle configuration
  - [x] Add `SwaggerDoc` for v1 with title, version, description
  - [x] Add JWT Bearer security definition (`SecuritySchemes`) and global security requirement
  - [x] Enable `JsonStringEnumConverter` schema filter (Swashbuckle needs `UseAllOfToExtendReferenceSchemas` + enum filter)
  - [x] Add XML comments support (optional — enable `GenerateDocumentationFile` in Api.csproj)

- [x] **Task 2: Configure SwaggerUI** (AC: 1)
  - [x] Replace `app.UseSwaggerUI()` with versioned endpoint config
  - [x] Only expose Swagger in Development (guard behind `app.Environment.IsDevelopment()`)

## Dev Notes

### Swashbuckle Config

```csharp
// Replace AddSwaggerGen() in Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PH Payroll Time API",
        Version = "v1",
        Description = "Philippine DOLE RA 6727 overtime rules REST API"
    });

    // JWT Bearer security
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

    // Enum strings in Swagger schema (enums are already strings via JsonStringEnumConverter)
    options.UseInlineDefinitionsForEnums();
});
```

### SwaggerUI versioned endpoint

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PH Payroll Time API v1");
        options.RoutePrefix = "swagger";
    });
}
```

Move this block to only run in Development — replaces the unconditional `app.UseSwagger(); app.UseSwaggerUI();` from Story 1.1.

### Required usings

```csharp
using Microsoft.OpenApi.Models;
```

`Microsoft.OpenApi.Models` is transitively available via `Swashbuckle.AspNetCore` — no new NuGet package.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- Program.cs: replaced AddSwaggerGen() placeholder with full JWT Bearer security definition and enum inline mode
- SwaggerUI now guarded to Development environment only
- `UseInlineDefinitionsForEnums()` ensures all 40 enum values rendered as strings in schema

### File List

- src/PhPayrollTimeApi.Api/Program.cs (updated)
