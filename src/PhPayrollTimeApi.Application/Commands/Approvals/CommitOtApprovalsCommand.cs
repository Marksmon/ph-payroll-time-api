using System.Text.Json;
using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Approvals;

public record CommitOtApprovalsCommand(
    IReadOnlyList<Guid> OtApprovalIds,
    string ActorSubClaim);

public class CommitOtApprovalsCommandHandler : ICommandHandler<CommitOtApprovalsCommand>
{
    private const int MaxBatch = 100;
    private readonly IApprovalQueueRepository _repo;
    private readonly IHolidayApprovalRepository _holidayApprovalRepo;
    private readonly IClockProvider _clock;

    public CommitOtApprovalsCommandHandler(
        IApprovalQueueRepository repo,
        IHolidayApprovalRepository holidayApprovalRepo,
        IClockProvider clock)
    {
        _repo = repo;
        _holidayApprovalRepo = holidayApprovalRepo;
        _clock = clock;
    }

    public async Task HandleAsync(CommitOtApprovalsCommand command, CancellationToken ct)
    {
        if (command.OtApprovalIds.Count > MaxBatch)
            throw new DomainValidationException($"Batch size cannot exceed {MaxBatch}.");

        var now = _clock.UtcNow;
        var toCommit = new List<OtApproval>();

        foreach (var id in command.OtApprovalIds)
        {
            var approval = await _repo.GetOtApprovalByIdAsync(id, ct)
                ?? throw new EntityNotFoundException("OtApproval", id);

            if (approval.Status != ApprovalStatus.PENDING)
                throw new StaleApprovalException($"OT Approval {id} is not in PENDING state.");

            // Prerequisite gate (FR36): check holiday/rest-day approvals are not pending for this schedule
            var holidayApproval = await _holidayApprovalRepo.GetHolidayScheduleApprovalAsync(approval.ShiftScheduleId, ct);
            if (holidayApproval?.Status == ApprovalStatus.PENDING)
                throw new ConflictException($"Holiday Schedule Approval for schedule {approval.ShiftScheduleId} must be resolved before committing OT.");

            var restDayApproval = await _holidayApprovalRepo.GetRestDayScheduleApprovalAsync(approval.ShiftScheduleId, ct);
            if (restDayApproval?.Status == ApprovalStatus.PENDING)
                throw new ConflictException($"Rest Day Schedule Approval for schedule {approval.ShiftScheduleId} must be resolved before committing OT.");

            toCommit.Add(approval);
        }

        // Atomic commit of all approvals + audit records in a single SaveChanges call
        foreach (var approval in toCommit)
        {
            approval.Status = ApprovalStatus.APPROVED;
            approval.CommittedBySubClaim = command.ActorSubClaim;
            approval.CommittedAt = now;
            approval.UpdatedAt = now;

            var audit = new AuditRecord
            {
                Id = Guid.NewGuid(),
                EntityType = "OtApproval",
                EntityId = approval.Id.ToString(),
                Action = "COMMITTED",
                ActorSubClaim = command.ActorSubClaim,
                Payload = JsonSerializer.Serialize(new
                {
                    approval.Id,
                    StagedActionCount = approval.StagedActions.Count,
                    StagedActions = approval.StagedActions.Select(s => new
                    {
                        s.Id, s.ActionType, s.SegmentClassification, s.AdjustedDurationMinutes, s.Reason
                    })
                }),
                OccurredAt = now
            };
            await _repo.AddAuditRecordAsync(audit, ct);
        }

        await _repo.SaveAsync(ct);
    }
}
