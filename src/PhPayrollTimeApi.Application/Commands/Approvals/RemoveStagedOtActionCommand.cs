using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Approvals;

public record RemoveStagedOtActionCommand(Guid StagedActionId);

public class RemoveStagedOtActionCommandHandler : ICommandHandler<RemoveStagedOtActionCommand>
{
    private readonly IApprovalQueueRepository _repo;

    public RemoveStagedOtActionCommandHandler(IApprovalQueueRepository repo) => _repo = repo;

    public async Task HandleAsync(RemoveStagedOtActionCommand command, CancellationToken ct)
    {
        var action = await _repo.GetStagedActionByIdAsync(command.StagedActionId, ct)
            ?? throw new EntityNotFoundException("StagedOtAction", command.StagedActionId);

        await _repo.RemoveStagedActionAsync(action, ct);
        await _repo.SaveAsync(ct);
    }
}
