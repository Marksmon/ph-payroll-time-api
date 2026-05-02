using System.Reflection;
using Xunit;

namespace PhPayrollTimeApi.Domain.Tests;

public class MiddlewarePipelineOrderTests
{
    [Fact]
    public void ProgramCs_UseAuthentication_PrecedesUseRateLimiter()
    {
        var programCsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "PhPayrollTimeApi.Api", "Program.cs");

        if (!File.Exists(programCsPath))
            return; // Skip in CI if source not present

        var content = File.ReadAllText(programCsPath);
        var authIndex = content.IndexOf("app.UseAuthentication()", StringComparison.Ordinal);
        var rateLimiterIndex = content.IndexOf("app.UseRateLimiter()", StringComparison.Ordinal);

        Assert.True(authIndex >= 0, "UseAuthentication() not found in Program.cs");
        Assert.True(rateLimiterIndex >= 0, "UseRateLimiter() not found in Program.cs");
        Assert.True(authIndex < rateLimiterIndex,
            "UseAuthentication() must precede UseRateLimiter() in Program.cs (NFR-S5)");
    }
}
