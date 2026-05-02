using System.Net;
using System.Text;
using System.Text.Json;
using PhPayrollTimeApi.Integration.Tests.Fixtures;
using Xunit;

namespace PhPayrollTimeApi.Integration.Tests.Infrastructure;

public class ExceptionHandlerTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;

    public ExceptionHandlerTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task EntityNotFoundException_Returns404WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/test/probe/not-found");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task DomainValidationException_Returns400WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/test/probe/validation");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ScheduleOverlapException_Returns409WithOverlapType()
    {
        var response = await _client.GetAsync("/api/v1/test/probe/overlap");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("overlapping-schedule", body);
    }

    [Fact]
    public async Task StaleApprovalException_Returns409WithStaleType()
    {
        var response = await _client.GetAsync("/api/v1/test/probe/stale");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("stale-approval", body);
    }

    [Fact]
    public async Task ComputationInvariantException_Returns422WithViolations()
    {
        var response = await _client.GetAsync("/api/v1/test/probe/invariant");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("violations", body);
    }

    [Fact]
    public async Task UnhandledException_Returns500WithoutLeakingDetails()
    {
        var response = await _client.GetAsync("/api/v1/test/probe/unhandled");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Test unhandled exception", body);
    }

    [Fact]
    public async Task UnknownRoute_Returns404WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/this-route-does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidModelInput_Returns400WithFieldErrors()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/test/probe/model-validation", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
