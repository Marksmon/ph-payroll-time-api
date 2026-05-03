using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.GetRestDayApprovalQueue;

public record GetRestDayApprovalQueueQuery(ApprovalStatus Status);

public class GetRestDayApprovalQueueQueryHandler
    : IQueryHandler<GetRestDayApprovalQueueQuery, List<RestDayScheduleApprovalDto>>
{
    private readonly IApprovalQueueRepository _repo;

    public GetRestDayApprovalQueueQueryHandler(IApprovalQueueRepository repo) => _repo = repo;

    public async Task<List<RestDayScheduleApprovalDto>> HandleAsync(
        GetRestDayApprovalQueueQuery query, CancellationToken ct)
    {
        var approvals = await _repo.GetRestDayApprovalsByStatusAsync(query.Status, ct);
        return approvals.Select(a => new RestDayScheduleApprovalDto(
            a.Id, a.EmployeeId, a.ShiftScheduleId, a.RestDayDate.ToString("yyyy-MM-dd"),
            a.Status.ToString(), a.ApprovedBySubClaim, a.ApprovedAt, a.CreatedAt, a.UpdatedAt))
            .ToList();
    }
}
