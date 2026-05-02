using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class SystemClockProvider : IClockProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
