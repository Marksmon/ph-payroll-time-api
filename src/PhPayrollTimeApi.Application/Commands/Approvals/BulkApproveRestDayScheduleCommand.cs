using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Approvals;

public record BulkApproveRestDayScheduleCommand(
    IReadOnlyList<Guid> ApprovalIds,
    ApprovalStatus Action,
    string ActorSubClaim);

public class BulkApproveRestDayScheduleCommandHandler : ICommandHandler<BulkApproveRestDayScheduleCommand>
{
    private const int MaxBatch = 100;
    private readonly IApprovalQueueRepository _repo;
    private readonly IClockProvider _clock;

    public BulkApproveRestDayScheduleCommandHandler(IApprovalQueueRepository repo, IClockProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task HandleAsync(BulkApproveRestDayScheduleCommand command, CancellationToken ct)
    {
        if (command.ApprovalIds.Count > MaxBatch)
            throw new DomainValidationException($"Batch size cannot exceed {MaxBatch} approvals.");

        foreach (var id in command.ApprovalIds)
        {
            var approval = await _repo.GetRestDayApprovalByIdAsync(id, ct)
                ?? throw new EntityNotFoundException("RestDayScheduleApproval", id);

            if (approval.Status != ApprovalStatus.PENDING)
                throw new ConflictException($"Approval {id} is already in state {approval.Status} and cannot be changed without first rejecting it.");

            approval.Status = command.Action;
            approval.ApprovedBySubClaim = command.ActorSubClaim;
            approval.ApprovedAt = _clock.UtcNow;
            approval.UpdatedAt = _clock.UtcNow;
        }

        await _repo.SaveAsync(ct);
    }
}
