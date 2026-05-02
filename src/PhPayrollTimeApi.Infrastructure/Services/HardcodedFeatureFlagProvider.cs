using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Infrastructure.Services;

// Stub for Epics 1-7. Switched to DbFeatureFlagProvider in Epic 8, Story 8.2.
public class HardcodedFeatureFlagProvider : IFeatureFlagProvider
{
    public Task<bool> IsEnabledAsync(string flagName, CancellationToken ct)
        => Task.FromResult(true);
}
