namespace PhPayrollTimeApi.Domain.Exceptions;

public class DomainValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public DomainValidationException(string message) : base(message)
        => Errors = new[] { message };

    public DomainValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
        => Errors = errors;
}
