namespace PhPayrollTimeApi.Api.Constants;

public static class ProblemTypes
{
    private const string Base = "https://ph-payroll-time-api/errors";

    public const string Validation = $"{Base}/validation";
    public const string Unauthorized = $"{Base}/unauthorized";
    public const string Forbidden = $"{Base}/forbidden";
    public const string NotFound = $"{Base}/not-found";
    public const string OverlappingSchedule = $"{Base}/conflict/overlapping-schedule";
    public const string StaleApproval = $"{Base}/conflict/stale-approval";
    public const string ComputationInvariant = $"{Base}/computation-invariant";
    public const string RateLimitExceeded = $"{Base}/rate-limit-exceeded";
    public const string InternalError = $"{Base}/internal-error";
}
