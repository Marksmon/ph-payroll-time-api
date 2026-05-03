using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.GetOtApprovalQueue;

public record GetOtApprovalQueueQuery;

public class GetOtApprovalQueueQueryHandler
    : IQueryHandler<GetOtApprovalQueueQuery, List<OtApprovalDto>>
{
    private readonly IApprovalQueueRepository _repo;

    public GetOtApprovalQueueQueryHandler(IApprovalQueueRepository repo) => _repo = repo;

    public async Task<List<OtApprovalDto>> HandleAsync(GetOtApprovalQueueQuery query, CancellationToken ct)
    {
        var approvals = await _repo.GetPendingOtApprovalsAsync(ct);
        return approvals.Select(ToDto).ToList();
    }

    private static OtApprovalDto ToDto(Domain.Entities.OtApproval a) => new(
        a.Id, a.EmployeeId, a.ShiftScheduleId,
        a.Status.ToString(), a.IsStale,
        a.CommittedBySubClaim, a.CommittedAt, a.CreatedAt, a.UpdatedAt,
        a.StagedActions.Select(s => new StagedOtActionDto(
            s.Id, s.OtApprovalId, s.ActionType.ToString(),
            s.SegmentClassification?.ToString(),
            s.AdjustedDurationMinutes, s.Reason, s.CreatedAt)).ToList());
}
