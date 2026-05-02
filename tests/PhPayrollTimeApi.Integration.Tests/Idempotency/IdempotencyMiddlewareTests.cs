using PhPayrollTimeApi.Api.Middleware;
using Xunit;

namespace PhPayrollTimeApi.Integration.Tests.Idempotency;

public class IdempotencyMiddlewareTests
{
    [Fact]
    public void ComputeCacheKey_SameInputs_ReturnsSameHash()
    {
        var key1 = IdempotencyMiddleware.ComputeCacheKey("POST", "/api/v1/timelogs", "abc-123");
        var key2 = IdempotencyMiddleware.ComputeCacheKey("POST", "/api/v1/timelogs", "abc-123");
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void ComputeCacheKey_DifferentIdempotencyKey_ReturnsDifferentHash()
    {
        var key1 = IdempotencyMiddleware.ComputeCacheKey("POST", "/api/v1/timelogs", "key-1");
        var key2 = IdempotencyMiddleware.ComputeCacheKey("POST", "/api/v1/timelogs", "key-2");
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeCacheKey_DifferentPath_ReturnsDifferentHash()
    {
        var key1 = IdempotencyMiddleware.ComputeCacheKey("POST", "/api/v1/timelogs", "abc-123");
        var key2 = IdempotencyMiddleware.ComputeCacheKey("POST", "/api/v1/approvals", "abc-123");
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeCacheKey_ReturnsHexString()
    {
        var key = IdempotencyMiddleware.ComputeCacheKey("POST", "/api/v1/timelogs", "abc-123");
        Assert.Matches("^[0-9A-F]{64}$", key);
    }
}
