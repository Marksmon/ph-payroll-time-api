using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhPayrollTimeApi.Integration.Tests.Fixtures;
using PhPayrollTimeApi.Infrastructure.Persistence;
using Xunit;

namespace PhPayrollTimeApi.Integration.Tests.TimeLogs;

public class TimeLogsControllerTests : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private IServiceScope _scope = null!;

    public TimeLogsControllerTests(ApiTestFixture fixture) => _fixture = fixture;

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

    private async Task<(string EmpId, string Sub)> CreateTestEmployee(HttpClient client)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var sub = $"tlog-{s}";
        var body = new
        {
            employeeNumber = $"TL-{s}",
            fullName = $"Log User {s}",
            role = "EMPLOYEE",
            jwtSubjectClaim = sub
        };
        var r = await client.PostAsJsonAsync("/api/v1/employees", body);
        r.EnsureSuccessStatusCode();
        var id = JsonDocument.Parse(await r.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;
        return (id, sub);
    }

    private static string IdempotencyKey() => Guid.NewGuid().ToString();

    private static HttpRequestMessage LogRequest(string empId, object body)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/employees/{empId}/logs");
        req.Headers.Add("Idempotency-Key", IdempotencyKey());
        req.Content = JsonContent.Create(body);
        return req;
    }

    [Fact]
    public async Task SubmitLog_AsEmployee_OwnId_Returns201()
    {
        var mgrClient = ManagerClient();
        var (empId, sub) = await CreateTestEmployee(mgrClient);

        var empClient = EmployeeClient(sub);
        var body = new { logType = "IN", loggedAt = (DateTimeOffset?)null };
        var response = await empClient.SendAsync(LogRequest(empId, body));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_AsEmployee_OtherEmployee_Returns403()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var intruder = EmployeeClient("tlog-intruder");
        var body = new { logType = "IN", loggedAt = (DateTimeOffset?)null };
        var response = await intruder.SendAsync(LogRequest(empId, body));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_AsManager_AnyEmployee_Returns201()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var body = new { logType = "OUT", loggedAt = (DateTimeOffset?)null };
        var response = await mgrClient.SendAsync(LogRequest(empId, body));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_WithExplicitPastTimestamp_Returns201()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var pastTime = DateTimeOffset.UtcNow.AddHours(-2);
        var body = new { logType = "IN", loggedAt = pastTime };
        var response = await mgrClient.SendAsync(LogRequest(empId, body));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_WithFutureTimestamp_Returns422()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var futureTime = DateTimeOffset.UtcNow.AddHours(2);
        var body = new { logType = "IN", loggedAt = futureTime };
        var response = await mgrClient.SendAsync(LogRequest(empId, body));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_WithoutIdempotencyKey_Returns400()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/employees/{empId}/logs");
        req.Content = JsonContent.Create(new { logType = "IN", loggedAt = (DateTimeOffset?)null });
        var response = await mgrClient.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_InvalidLogType_Returns400()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var body = new { logType = "INVALID", loggedAt = (DateTimeOffset?)null };
        var response = await mgrClient.SendAsync(LogRequest(empId, body));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_NonExistentEmployee_Returns404()
    {
        var mgrClient = ManagerClient();
        var fakeId = Guid.NewGuid();
        var body = new { logType = "IN", loggedAt = (DateTimeOffset?)null };
        var response = await mgrClient.SendAsync(LogRequest(fakeId.ToString(), body));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitLog_ResponseContainsId()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var body = new { logType = "IN", loggedAt = (DateTimeOffset?)null };
        var response = await mgrClient.SendAsync(LogRequest(empId, body));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("id", out _));
        Assert.True(doc.RootElement.TryGetProperty("loggedAt", out _));
    }

    private static string Iso(DateTimeOffset dt) => Uri.EscapeDataString(dt.UtcDateTime.ToString("O"));

    [Fact]
    public async Task GetLogs_AsManager_ReturnsAll()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        await mgrClient.SendAsync(LogRequest(empId, new { logType = "IN", loggedAt = (DateTimeOffset?)null }));

        var start = Iso(DateTimeOffset.UtcNow.AddDays(-1));
        var end = Iso(DateTimeOffset.UtcNow.AddDays(1));
        var response = await mgrClient.GetAsync(
            $"/api/v1/employees/{empId}/logs?startDate={start}&endDate={end}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(arr.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetLogs_AsEmployee_OwnId_Returns200()
    {
        var mgrClient = ManagerClient();
        var (empId, sub) = await CreateTestEmployee(mgrClient);

        var empClient = EmployeeClient(sub);
        var start = Iso(DateTimeOffset.UtcNow.AddDays(-1));
        var end = Iso(DateTimeOffset.UtcNow.AddDays(1));
        var response = await empClient.GetAsync(
            $"/api/v1/employees/{empId}/logs?startDate={start}&endDate={end}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_AsEmployee_OtherEmployee_Returns403()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var intruder = EmployeeClient("tlog-noAccess");
        var start = Iso(DateTimeOffset.UtcNow.AddDays(-1));
        var end = Iso(DateTimeOffset.UtcNow.AddDays(1));
        var response = await intruder.GetAsync(
            $"/api/v1/employees/{empId}/logs?startDate={start}&endDate={end}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetLogs_NoLogsInRange_ReturnsEmptyArray()
    {
        var mgrClient = ManagerClient();
        var (empId, _) = await CreateTestEmployee(mgrClient);

        var start = Iso(new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var end = Iso(new DateTimeOffset(2099, 12, 31, 0, 0, 0, TimeSpan.Zero));
        var response = await mgrClient.GetAsync(
            $"/api/v1/employees/{empId}/logs?startDate={start}&endDate={end}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetLogs_Unauthenticated_Returns401()
    {
        var response = await _fixture.CreateClient().GetAsync(
            $"/api/v1/employees/{Guid.NewGuid()}/logs?startDate=2026-01-01T00:00:00Z&endDate=2026-12-31T00:00:00Z");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
