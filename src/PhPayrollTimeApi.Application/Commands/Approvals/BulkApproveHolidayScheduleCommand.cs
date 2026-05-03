using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Approvals;

public record BulkApproveHolidayScheduleCommand(
    IReadOnlyList<Guid> ApprovalIds,
    ApprovalStatus Action,
    string ActorSubClaim);

public class BulkApproveHolidayScheduleCommandHandler : ICommandHandler<BulkApproveHolidayScheduleCommand>
{
    private const int MaxBatch = 100;
    private readonly IApprovalQueueRepository _repo;
    private readonly IClockProvider _clock;

    public BulkApproveHolidayScheduleCommandHandler(IApprovalQueueRepository repo, IClockProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task HandleAsync(BulkApproveHolidayScheduleCommand command, CancellationToken ct)
    {
        if (command.ApprovalIds.Count > MaxBatch)
            throw new DomainValidationException($"Batch size cannot exceed {MaxBatch} approvals.");

        foreach (var id in command.ApprovalIds)
        {
            var approval = await _repo.GetHolidayApprovalByIdAsync(id, ct)
                ?? throw new EntityNotFoundException("HolidayScheduleApproval", id);

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
