namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IFeatureFlagProvider
{
    Task<bool> IsEnabledAsync(string flagName, CancellationToken ct);
}
