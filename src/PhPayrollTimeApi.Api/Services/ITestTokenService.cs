namespace PhPayrollTimeApi.Api.Services;

public interface ITestTokenService
{
    string GenerateToken(string sub, string role, int expiryHours = 1);
}
