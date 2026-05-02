using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Integration.Tests.Fixtures;

public class ApiTestFixture : WebApplicationFactory<Program>
{
    public static readonly RSA TestRsa = RSA.Create(2048);
    public static readonly RsaSecurityKey TestSecurityKey = new(TestRsa);

    public const string TestIssuer = "ph-payroll-time-api";
    public const string TestAudience = "ph-payroll-time-api-clients";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Replace real DbContext with test database
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    "Host=localhost;Port=5432;Database=ph_payroll_time_test_db;Username=postgres;Password=postgres"));

            // Register test-only controllers (ErrorProbeController etc.) from this assembly
            services.AddControllers()
                .AddApplicationPart(typeof(ApiTestFixture).Assembly);

            // Override JWT bearer to use in-memory test key — runs after Program.cs auth config
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
        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        SigningCredentials signingCredentials = algorithm == SecurityAlgorithms.RsaSha256
            ? new SigningCredentials(TestSecurityKey, SecurityAlgorithms.RsaSha256)
            : new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-hmac-key-not-valid-for-rs256")),
                algorithm);

        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
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
            SigningCredentials = signingCredentials
        };
        return handler.CreateToken(descriptor);
    }
}
