namespace PhPayrollTimeApi.Domain.Exceptions;

public class ScheduleOverlapException : Exception
{
    public ScheduleOverlapException(string message) : base(message) { }
}
