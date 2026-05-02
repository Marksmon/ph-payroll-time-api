namespace PhPayrollTimeApi.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityType, object id)
        : base($"{entityType} with ID '{id}' was not found.") { }

    public EntityNotFoundException(string message) : base(message) { }
}
