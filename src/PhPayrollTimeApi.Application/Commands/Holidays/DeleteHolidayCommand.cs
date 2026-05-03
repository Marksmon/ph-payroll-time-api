using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Holidays;

public record DeleteHolidayCommand(DateOnly Date);

public class DeleteHolidayCommandHandler : ICommandHandler<DeleteHolidayCommand>
{
    private readonly IHolidayRepository _repo;
    private readonly IApprovalQueueRepository _approvalQueue;

    public DeleteHolidayCommandHandler(IHolidayRepository repo, IApprovalQueueRepository approvalQueue)
    {
        _repo = repo;
        _approvalQueue = approvalQueue;
    }

    public async Task HandleAsync(DeleteHolidayCommand cmd, CancellationToken ct)
    {
        var entry = await _repo.GetByDateAsync(cmd.Date, ct)
            ?? throw new EntityNotFoundException($"No holiday entry found for {cmd.Date:yyyy-MM-dd}.");

        var now = DateTimeOffset.UtcNow;

        // Story 6.6: flag stale OT approvals for this date before deleting the holiday
        var stale = await _approvalQueue.GetOtApprovalsByDateAsync(cmd.Date, ct);
        foreach (var ot in stale)
        {
            ot.IsStale = true;
            ot.UpdatedAt = now;
        }

        await _repo.DeleteAsync(entry, ct);
        await _repo.SaveAsync(ct);
    }
}
