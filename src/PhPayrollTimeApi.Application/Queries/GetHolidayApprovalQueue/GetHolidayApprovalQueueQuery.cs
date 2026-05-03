using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Application.Dtos;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Queries.GetHolidayApprovalQueue;

public record GetHolidayApprovalQueueQuery(ApprovalStatus Status);

public class GetHolidayApprovalQueueQueryHandler
    : IQueryHandler<GetHolidayApprovalQueueQuery, List<HolidayScheduleApprovalDto>>
{
    private readonly IApprovalQueueRepository _repo;

    public GetHolidayApprovalQueueQueryHandler(IApprovalQueueRepository repo) => _repo = repo;

    public async Task<List<HolidayScheduleApprovalDto>> HandleAsync(
        GetHolidayApprovalQueueQuery query, CancellationToken ct)
    {
        var approvals = await _repo.GetHolidayApprovalsByStatusAsync(query.Status, ct);
        return approvals.Select(a => new HolidayScheduleApprovalDto(
            a.Id, a.EmployeeId, a.ShiftScheduleId, a.HolidayDate.ToString("yyyy-MM-dd"),
            a.Status.ToString(), a.ApprovedBySubClaim, a.ApprovedAt, a.CreatedAt, a.UpdatedAt))
            .ToList();
    }
}
