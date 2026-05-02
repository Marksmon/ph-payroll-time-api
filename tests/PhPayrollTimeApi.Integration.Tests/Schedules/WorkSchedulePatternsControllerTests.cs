using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhPayrollTimeApi.Integration.Tests.Fixtures;
using PhPayrollTimeApi.Infrastructure.Persistence;
using Xunit;

namespace PhPayrollTimeApi.Integration.Tests.Schedules;

public class WorkSchedulePatternsControllerTests : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private IServiceScope _scope = null!;

    public WorkSchedulePatternsControllerTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _scope = _fixture.Services.CreateScope();
        var db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient ManagerClient() =>
        AuthClient(_fixture.GenerateTestToken(sub: "mgr-001", role: "MANAGER"));

    private HttpClient HrClient() =>
        AuthClient(_fixture.GenerateTestToken(sub: "hr-001", role: "HR_ADMIN"));

    private HttpClient EmployeeClient(string sub) =>
        AuthClient(_fixture.GenerateTestToken(sub: sub, role: "EMPLOYEE"));

    private HttpClient AuthClient(string token)
    {
        var c = _fixture.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<string> CreateTestEmployee(HttpClient client)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var body = new
        {
            employeeNumber = $"WP-{s}",
            fullName = $"Pattern User {s}",
            role = "EMPLOYEE",
            jwtSubjectClaim = $"wpat-{s}"
        };
        var r = await client.PostAsJsonAsync("/api/v1/employees", body);
        r.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await r.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task AssignPattern_AsManager_Returns201()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);

        var body = new
        {
            restDays = new[] { 0, 6 },
            effectiveDate = "2026-06-01",
            expiryDate = (string?)null
        };
        var response = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedule-patterns", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AssignPattern_AsEmployee_Returns403()
    {
        var mgrClient = ManagerClient();
        var empId = await CreateTestEmployee(mgrClient);
        var empClient = EmployeeClient("some-emp-wp");

        var body = new { restDays = new[] { 0 }, effectiveDate = "2026-06-01", expiryDate = (string?)null };
        var response = await empClient.PostAsJsonAsync($"/api/v1/employees/{empId}/schedule-patterns", body);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignPattern_ExpiryBeforeEffective_Returns400()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);

        var body = new
        {
            restDays = new[] { 0 },
            effectiveDate = "2026-06-15",
            expiryDate = "2026-06-01"
        };
        var response = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedule-patterns", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPatterns_AsManager_ReturnsAll()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);

        var body = new { restDays = new[] { 6 }, effectiveDate = "2026-07-01", expiryDate = (string?)null };
        await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedule-patterns", body);

        var response = await client.GetAsync($"/api/v1/employees/{empId}/schedule-patterns");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(arr.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetPatterns_AsEmployee_OtherEmployee_Returns403()
    {
        var mgrClient = ManagerClient();
        var empId = await CreateTestEmployee(mgrClient);
        var empClient = EmployeeClient("wp-intruder");

        var response = await empClient.GetAsync($"/api/v1/employees/{empId}/schedule-patterns");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePattern_AsManager_Returns200()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);

        var assign = new { restDays = new[] { 0 }, effectiveDate = "2026-08-01", expiryDate = (string?)null };
        var create = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedule-patterns", assign);
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var patternId = created.RootElement.GetProperty("id").GetString();

        var update = new { effectiveDate = "2026-08-15", expiryDate = "2026-12-31" };
        var response = await client.PutAsJsonAsync(
            $"/api/v1/employees/{empId}/schedule-patterns/{patternId}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BulkAssign_AsManager_Returns200()
    {
        var client = ManagerClient();
        var empId1 = await CreateTestEmployee(client);
        var empId2 = await CreateTestEmployee(client);

        var body = new
        {
            employeeIds = new[] { empId1, empId2 },
            restDays = new[] { 0, 6 },
            effectiveDate = "2026-09-01",
            expiryDate = (string?)null
        };
        var response = await client.PostAsJsonAsync("/api/v1/employees/schedule-patterns/bulk", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
