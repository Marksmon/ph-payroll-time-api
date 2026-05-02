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

public class ShiftSchedulesControllerTests : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private IServiceScope _scope = null!;

    public ShiftSchedulesControllerTests(ApiTestFixture fixture) => _fixture = fixture;

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

    private async Task<string> CreateTestEmployee(HttpClient client, string? suffix = null)
    {
        var s = suffix ?? Guid.NewGuid().ToString("N")[..8];
        var body = new
        {
            employeeNumber = $"SE-{s}",
            fullName = $"Sched User {s}",
            role = "EMPLOYEE",
            jwtSubjectClaim = $"sched-{s}"
        };
        var r = await client.PostAsJsonAsync("/api/v1/employees", body);
        r.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    private static object DayShift(int year = 2026) => new
    {
        scheduleStart = new DateTimeOffset(year, 6, 1, 0, 0, 0, TimeSpan.Zero),
        scheduleEnd = new DateTimeOffset(year, 6, 1, 9, 0, 0, TimeSpan.Zero),
        breakWindows = new[]
        {
            new
            {
                breakStart = new DateTimeOffset(year, 6, 1, 4, 0, 0, TimeSpan.Zero),
                breakEnd = new DateTimeOffset(year, 6, 1, 5, 0, 0, TimeSpan.Zero)
            }
        }
    };

    [Fact]
    public async Task CreateSchedule_AsManager_Returns201()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);
        var response = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", DayShift());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_AsEmployee_Returns403()
    {
        var mgrClient = ManagerClient();
        var empId = await CreateTestEmployee(mgrClient);

        var empClient = EmployeeClient("some-emp");
        var response = await empClient.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", DayShift(2027));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_WithBreakWindow_PersistsBreakWindow()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);
        var response = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", DayShift(2028));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("breakWindows").GetArrayLength());
    }

    [Fact]
    public async Task CreateSchedule_OverlappingDates_Returns409()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);

        await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", DayShift(2029));

        // Second schedule overlaps the first
        var overlap = new
        {
            scheduleStart = new DateTimeOffset(2029, 6, 1, 4, 0, 0, TimeSpan.Zero),
            scheduleEnd = new DateTimeOffset(2029, 6, 1, 14, 0, 0, TimeSpan.Zero),
            breakWindows = Array.Empty<object>()
        };
        var response = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", overlap);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("overlapping-schedule", text);
    }

    [Fact]
    public async Task GetSchedules_AsManager_ReturnsAll()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);
        await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", DayShift(2030));

        var response = await client.GetAsync($"/api/v1/employees/{empId}/schedules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetSchedules_AsEmployee_OwnId_Returns200()
    {
        var mgrClient = ManagerClient();
        var s = Guid.NewGuid().ToString("N")[..8];
        var body = new
        {
            employeeNumber = $"OWN-{s}",
            fullName = $"Own User {s}",
            role = "EMPLOYEE",
            jwtSubjectClaim = $"own-{s}"
        };
        var create = await mgrClient.PostAsJsonAsync("/api/v1/employees", body);
        var empDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var empId = empDoc.RootElement.GetProperty("id").GetString()!;

        var empClient = EmployeeClient($"own-{s}");
        var response = await empClient.GetAsync($"/api/v1/employees/{empId}/schedules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_AsEmployee_OtherEmployee_Returns403()
    {
        var mgrClient = ManagerClient();
        var empId = await CreateTestEmployee(mgrClient);

        var empClient = EmployeeClient("emp-not-owner");
        var response = await empClient.GetAsync($"/api/v1/employees/{empId}/schedules");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedules_EmptyEmployee_ReturnsEmptyArray()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);

        var response = await client.GetAsync($"/api/v1/employees/{empId}/schedules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task DeleteSchedule_AsManager_Returns204()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);
        var create = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", DayShift(2031));
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var schedId = created.RootElement.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/v1/employees/{empId}/schedules/{schedId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSchedule_AsManager_Returns200()
    {
        var client = ManagerClient();
        var empId = await CreateTestEmployee(client);
        var create = await client.PostAsJsonAsync($"/api/v1/employees/{empId}/schedules", DayShift(2032));
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var schedId = created.RootElement.GetProperty("id").GetString();

        var updated = new
        {
            scheduleStart = new DateTimeOffset(2032, 6, 1, 1, 0, 0, TimeSpan.Zero),
            scheduleEnd = new DateTimeOffset(2032, 6, 1, 10, 0, 0, TimeSpan.Zero),
            breakWindows = Array.Empty<object>()
        };
        var response = await client.PutAsJsonAsync($"/api/v1/employees/{empId}/schedules/{schedId}", updated);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
