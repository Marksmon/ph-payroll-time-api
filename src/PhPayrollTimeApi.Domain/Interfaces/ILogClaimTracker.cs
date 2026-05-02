namespace PhPayrollTimeApi.Domain.Interfaces;

public interface ILogClaimTracker
{
    void Claim(Guid timeLogId);
    bool IsClaimed(Guid timeLogId);
}
