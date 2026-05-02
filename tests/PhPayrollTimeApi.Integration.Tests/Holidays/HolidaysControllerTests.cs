using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhPayrollTimeApi.Integration.Tests.Fixtures;
using PhPayrollTimeApi.Infrastructure.Persistence;
using Xunit;

namespace PhPayrollTimeApi.Integration.Tests.Holidays;

public class HolidaysControllerTests : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private IServiceScope _scope = null!;

    public HolidaysControllerTests(ApiTestFixture fixture) => _fixture = fixture;

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

    private HttpClient HrClient() =>
        AuthClient(_fixture.GenerateTestToken(sub: "hr-001", role: "HR_ADMIN"));

    private HttpClient ManagerClient() =>
        AuthClient(_fixture.GenerateTestToken(sub: "mgr-001", role: "MANAGER"));

    private HttpClient EmployeeClient() =>
        AuthClient(_fixture.GenerateTestToken(sub: "emp-001", role: "EMPLOYEE"));

    private HttpClient AuthClient(string token)
    {
        var c = _fixture.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    // Use far-future years to avoid collisions with seeded data
    private static int NextYear() => 2050 + Random.Shared.Next(0, 100);

    [Fact]
    public async Task CreateHoliday_AsHrAdmin_Returns201()
    {
        var yr = NextYear();
        var body = new { date = $"{yr}-03-15", name = "Test Holiday", type = "REGULAR" };
        var response = await HrClient().PostAsJsonAsync("/api/v1/holidays", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateHoliday_AsManager_Returns403()
    {
        var yr = NextYear();
        var body = new { date = $"{yr}-04-01", name = "Test", type = "REGULAR" };
        var response = await ManagerClient().PostAsJsonAsync("/api/v1/holidays", body);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateHoliday_AsEmployee_Returns403()
    {
        var yr = NextYear();
        var body = new { date = $"{yr}-05-01", name = "Test", type = "SPECIAL_NON_WORKING" };
        var response = await EmployeeClient().PostAsJsonAsync("/api/v1/holidays", body);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateHoliday_DuplicateDate_Returns409()
    {
        var hr = HrClient();
        var yr = NextYear();
        var body = new { date = $"{yr}-06-12", name = "Independence Day", type = "REGULAR" };
        await hr.PostAsJsonAsync("/api/v1/holidays", body);

        var duplicate = new { date = $"{yr}-06-12", name = "Another Holiday", type = "SPECIAL_NON_WORKING" };
        var response = await hr.PostAsJsonAsync("/api/v1/holidays", duplicate);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateHoliday_InvalidType_Returns400()
    {
        var yr = NextYear();
        var body = new { date = $"{yr}-07-04", name = "Bad Type", type = "INVALID_TYPE" };
        var response = await HrClient().PostAsJsonAsync("/api/v1/holidays", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHolidays_AuthenticatedUser_ReturnsEntriesInRange()
    {
        var hr = HrClient();
        var yr = NextYear();
        var body = new { date = $"{yr}-08-21", name = "Ninoy Aquino Day", type = "SPECIAL_NON_WORKING" };
        await hr.PostAsJsonAsync("/api/v1/holidays", body);

        var response = await ManagerClient().GetAsync(
            $"/api/v1/holidays?startDate={yr}-01-01&endDate={yr}-12-31");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(arr.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetHolidays_StartDateAfterEndDate_Returns400()
    {
        var response = await ManagerClient().GetAsync(
            "/api/v1/holidays?startDate=2026-12-31&endDate=2026-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetHolidays_NoEntriesInRange_ReturnsEmptyArray()
    {
        var response = await EmployeeClient().GetAsync(
            "/api/v1/holidays?startDate=2200-01-01&endDate=2200-12-31");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, arr.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetHolidays_Unauthenticated_Returns401()
    {
        var response = await _fixture.CreateClient().GetAsync(
            "/api/v1/holidays?startDate=2026-01-01&endDate=2026-12-31");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHoliday_AsHrAdmin_Returns200()
    {
        var hr = HrClient();
        var yr = NextYear();
        var date = $"{yr}-09-01";
        await hr.PostAsJsonAsync("/api/v1/holidays", new { date, name = "Original", type = "REGULAR" });

        var update = new { name = "Updated National Heroes Day", type = "SPECIAL_NON_WORKING" };
        var response = await hr.PutAsJsonAsync($"/api/v1/holidays/{date}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHoliday_NonExistentDate_Returns404()
    {
        var response = await HrClient().PutAsJsonAsync("/api/v1/holidays/2099-12-25",
            new { name = "Christmas", type = "REGULAR" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteHoliday_AsHrAdmin_Returns204()
    {
        var hr = HrClient();
        var yr = NextYear();
        var date = $"{yr}-10-31";
        await hr.PostAsJsonAsync("/api/v1/holidays", new { date, name = "Halloween", type = "SPECIAL_NON_WORKING" });

        var response = await hr.DeleteAsync($"/api/v1/holidays/{date}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteHoliday_AsManager_Returns403()
    {
        var response = await ManagerClient().DeleteAsync("/api/v1/holidays/2026-12-25");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task HolidayTypes_AllFourTypesAccepted()
    {
        var hr = HrClient();
        var yr = NextYear();
        foreach (var (type, month) in new[] { ("REGULAR", 1), ("SPECIAL_NON_WORKING", 2), ("SPECIAL_WORKING", 3), ("NONE", 4) })
        {
            var body = new { date = $"{yr}-{month:D2}-01", name = $"Test {type}", type };
            var response = await hr.PostAsJsonAsync("/api/v1/holidays", body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
