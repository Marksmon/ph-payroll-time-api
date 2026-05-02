namespace PhPayrollTimeApi.Domain.Exceptions;

public class StaleApprovalException : Exception
{
    public StaleApprovalException(string message) : base(message) { }
}
