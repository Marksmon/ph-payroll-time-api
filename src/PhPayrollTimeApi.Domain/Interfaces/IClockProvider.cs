namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IClockProvider
{
    DateTimeOffset UtcNow { get; }
}
