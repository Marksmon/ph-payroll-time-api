using System.Net;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using PhPayrollTimeApi.Integration.Tests.Fixtures;
using Xunit;

namespace PhPayrollTimeApi.Integration.Tests.Auth;

public class JwtAuthenticationTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public JwtAuthenticationTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient CreateClient() => _fixture.CreateClient();

    [Fact]
    public async Task Request_WithNoAuthHeader_Returns401WithProblemDetails()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Request_WithExpiredToken_Returns401()
    {
        var client = CreateClient();
        var token = _fixture.GenerateTestToken(expired: true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithHs256Token_Returns401()
    {
        var client = CreateClient();
        var token = _fixture.GenerateTestToken(algorithm: SecurityAlgorithms.HmacSha256);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidRs256Token_Returns200()
    {
        var client = CreateClient();
        var token = _fixture.GenerateTestToken(sub: "emp-001", role: "EMPLOYEE");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithWrongIssuer_Returns401()
    {
        var client = CreateClient();
        var token = _fixture.GenerateTestToken(issuer: "wrong-issuer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithWrongAudience_Returns401()
    {
        var client = CreateClient();
        var token = _fixture.GenerateTestToken(audience: "wrong-audience");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_SubAndRoleClaimsArePreserved()
    {
        var client = CreateClient();
        var token = _fixture.GenerateTestToken(sub: "mgr-001", role: "MANAGER");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("mgr-001", body);
        Assert.Contains("MANAGER", body);
    }
}
