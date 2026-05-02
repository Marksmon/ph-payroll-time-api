using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Infrastructure.Services;

// Scoped per request — tracks which TimeLog IDs have been claimed during a single Compute() call
public class InMemoryLogClaimTracker : ILogClaimTracker
{
    private readonly HashSet<Guid> _claimed = new();

    public void Claim(Guid timeLogId) => _claimed.Add(timeLogId);
    public bool IsClaimed(Guid timeLogId) => _claimed.Contains(timeLogId);
}
