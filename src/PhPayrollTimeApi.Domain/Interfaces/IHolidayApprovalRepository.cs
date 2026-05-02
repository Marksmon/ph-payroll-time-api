using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Domain.Interfaces;

public interface IHolidayApprovalRepository
{
    Task<HolidayScheduleApproval?> GetHolidayScheduleApprovalAsync(
        Guid shiftScheduleId, CancellationToken ct);

    Task<RestDayScheduleApproval?> GetRestDayScheduleApprovalAsync(
        Guid shiftScheduleId, CancellationToken ct);

    Task<IReadOnlyList<HolidayScheduleApproval>> GetHolidayScheduleApprovalsByEmployeeAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct);

    Task<IReadOnlyList<RestDayScheduleApproval>> GetRestDayScheduleApprovalsByEmployeeAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct);
}
