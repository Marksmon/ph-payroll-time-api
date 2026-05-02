namespace PhPayrollTimeApi.Application.Abstractions;

public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}
