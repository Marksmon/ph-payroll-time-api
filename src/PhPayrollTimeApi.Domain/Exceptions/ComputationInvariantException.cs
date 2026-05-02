namespace PhPayrollTimeApi.Domain.Exceptions;

public class ComputationInvariantException : Exception
{
    public IReadOnlyList<string> Violations { get; }

    public ComputationInvariantException(IReadOnlyList<string> violations)
        : base($"Computation invariant violations: {string.Join("; ", violations)}")
        => Violations = violations;
}
