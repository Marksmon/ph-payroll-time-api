using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Holidays;

public record UpdateHolidayCommand(DateOnly Date, string Name, HolidayType Type);

public class UpdateHolidayCommandHandler : ICommandHandler<UpdateHolidayCommand>
{
    private readonly IHolidayRepository _repo;
    private readonly IApprovalQueueRepository _approvalQueue;

    public UpdateHolidayCommandHandler(IHolidayRepository repo, IApprovalQueueRepository approvalQueue)
    {
        _repo = repo;
        _approvalQueue = approvalQueue;
    }

    public async Task HandleAsync(UpdateHolidayCommand cmd, CancellationToken ct)
    {
        var entry = await _repo.GetByDateAsync(cmd.Date, ct)
            ?? throw new EntityNotFoundException($"No holiday entry found for {cmd.Date:yyyy-MM-dd}.");

        var now = DateTimeOffset.UtcNow;
        entry.Name = cmd.Name;
        entry.Type = cmd.Type;
        entry.UpdatedAt = now;

        // Story 6.6: flag stale OT approvals for this date
        var stale = await _approvalQueue.GetOtApprovalsByDateAsync(cmd.Date, ct);
        foreach (var ot in stale)
        {
            ot.IsStale = true;
            ot.UpdatedAt = now;
        }

        await _repo.SaveAsync(ct);
    }
}
