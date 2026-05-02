namespace PhPayrollTimeApi.Domain.Exceptions;

public class ConflictException : Exception
{
    public string ConflictType { get; }

    public ConflictException(string message, string conflictType = "conflict") : base(message)
        => ConflictType = conflictType;
}
