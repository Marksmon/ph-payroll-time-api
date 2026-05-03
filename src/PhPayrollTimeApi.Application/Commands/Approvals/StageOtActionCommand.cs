using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Approvals;

public record StageOtActionCommand(
    Guid OtApprovalId,
    StagedOtActionType ActionType,
    TimeSegmentClassification? SegmentClassification,
    int? AdjustedDurationMinutes,
    string? Reason);

public class StageOtActionCommandHandler : ICommandHandler<StageOtActionCommand>
{
    private readonly IApprovalQueueRepository _repo;
    private readonly IClockProvider _clock;

    public StageOtActionCommandHandler(IApprovalQueueRepository repo, IClockProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task HandleAsync(StageOtActionCommand command, CancellationToken ct)
    {
        var approval = await _repo.GetOtApprovalByIdAsync(command.OtApprovalId, ct)
            ?? throw new EntityNotFoundException("OtApproval", command.OtApprovalId);

        if (approval.Status != ApprovalStatus.PENDING)
            throw new ConflictException($"OT Approval {command.OtApprovalId} is not in PENDING state.");

        var action = new StagedOtAction
        {
            Id = Guid.NewGuid(),
            OtApprovalId = command.OtApprovalId,
            ActionType = command.ActionType,
            SegmentClassification = command.SegmentClassification,
            AdjustedDurationMinutes = command.AdjustedDurationMinutes,
            Reason = command.Reason,
            CreatedAt = _clock.UtcNow
        };
        approval.StagedActions.Add(action);
        approval.UpdatedAt = _clock.UtcNow;

        await _repo.SaveAsync(ct);
    }
}
