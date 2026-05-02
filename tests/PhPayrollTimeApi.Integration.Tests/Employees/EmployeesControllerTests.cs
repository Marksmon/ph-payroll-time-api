using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhPayrollTimeApi.Integration.Tests.Fixtures;
using PhPayrollTimeApi.Infrastructure.Persistence;
using Xunit;

namespace PhPayrollTimeApi.Integration.Tests.Employees;

public class EmployeesControllerTests : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private IServiceScope _scope = null!;
    private AppDbContext _db = null!;

    public EmployeesControllerTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _scope = _fixture.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await _db.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient ManagerClient()
    {
        var c = _fixture.CreateClient();
        c.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.GenerateTestToken(sub: "mgr-001", role: "MANAGER"));
        return c;
    }

    private HttpClient HrAdminClient()
    {
        var c = _fixture.CreateClient();
        c.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.GenerateTestToken(sub: "hr-001", role: "HR_ADMIN"));
        return c;
    }

    private HttpClient EmployeeClient(string sub = "emp-001")
    {
        var c = _fixture.CreateClient();
        c.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.GenerateTestToken(sub: sub, role: "EMPLOYEE"));
        return c;
    }

    private static object NewEmployeeBody(string? suffix = null)
    {
        var s = suffix ?? Guid.NewGuid().ToString("N")[..8];
        return new
        {
            employeeNumber = $"E-{s}",
            fullName = $"Test User {s}",
            role = "EMPLOYEE",
            jwtSubjectClaim = $"sub-{s}"
        };
    }

    [Fact]
    public async Task CreateEmployee_AsManager_Returns201()
    {
        var client = ManagerClient();
        var response = await client.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CreateEmployee_AsHrAdmin_Returns201()
    {
        var client = HrAdminClient();
        var response = await client.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_AsEmployee_Returns403()
    {
        var client = EmployeeClient();
        var response = await client.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_MissingRequiredFields_Returns400()
    {
        var client = ManagerClient();
        var response = await client.PostAsJsonAsync("/api/v1/employees", new { fullName = "Only Name" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetEmployee_WithValidId_Returns200()
    {
        var client = ManagerClient();
        var create = await client.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody());
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/v1/employees/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployee_NonExistentId_Returns404()
    {
        var client = ManagerClient();
        var response = await client.GetAsync($"/api/v1/employees/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_AsHrAdmin_Returns200()
    {
        var hrClient = HrAdminClient();
        var s = Guid.NewGuid().ToString("N")[..8];
        var create = await hrClient.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody(s));
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        var updateBody = new
        {
            fullName = "Updated Name",
            employeeNumber = $"E-{s}-upd",
            jwtSubjectClaim = $"sub-{s}-upd"
        };
        var response = await hrClient.PutAsJsonAsync($"/api/v1/employees/{id}", updateBody);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Updated Name", body.RootElement.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task UpdateEmployee_AsManager_Returns403()
    {
        var hrClient = HrAdminClient();
        var mgrClient = ManagerClient();

        var create = await hrClient.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody());
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        var response = await mgrClient.PutAsJsonAsync($"/api/v1/employees/{id}",
            new { fullName = "X", employeeNumber = "Y", jwtSubjectClaim = "Z" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateEmployee_AsHrAdmin_Returns204AndSoftDeletes()
    {
        var hrClient = HrAdminClient();
        var create = await hrClient.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody());
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        var deleteResponse = await hrClient.DeleteAsync($"/api/v1/employees/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Employee is soft-deleted — still retrievable but isActive = false
        var getResponse = await hrClient.GetAsync($"/api/v1/employees/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task DeactivateEmployee_AsEmployee_Returns403()
    {
        var hrClient = HrAdminClient();
        var create = await hrClient.PostAsJsonAsync("/api/v1/employees", NewEmployeeBody());
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        var empClient = EmployeeClient();
        var response = await empClient.DeleteAsync($"/api/v1/employees/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateEmployeeNumber_Returns400()
    {
        var client = ManagerClient();
        var s = Guid.NewGuid().ToString("N")[..8];
        var body = NewEmployeeBody(s);
        await client.PostAsJsonAsync("/api/v1/employees", body);

        // Second create with same employee number but different JWT sub
        var duplicate = new
        {
            employeeNumber = $"E-{s}",
            fullName = "Duplicate",
            role = "EMPLOYEE",
            jwtSubjectClaim = $"sub-{s}-2"
        };
        var response = await client.PostAsJsonAsync("/api/v1/employees", duplicate);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
