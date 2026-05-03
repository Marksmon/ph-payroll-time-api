using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Holidays;

public record CreateHolidayCommand(Guid Id, DateOnly Date, string Name, HolidayType Type);

public class CreateHolidayCommandHandler : ICommandHandler<CreateHolidayCommand>
{
    private readonly IHolidayRepository _repo;
    private readonly IApprovalQueueRepository _approvalQueue;

    public CreateHolidayCommandHandler(IHolidayRepository repo, IApprovalQueueRepository approvalQueue)
    {
        _repo = repo;
        _approvalQueue = approvalQueue;
    }

    public async Task HandleAsync(CreateHolidayCommand cmd, CancellationToken ct)
    {
        if (await _repo.DateExistsAsync(cmd.Date, ct))
            throw new ConflictException($"A holiday entry for {cmd.Date:yyyy-MM-dd} already exists.");

        var now = DateTimeOffset.UtcNow;
        await _repo.AddAsync(new HolidayCalendarEntry
        {
            Id = cmd.Id,
            Date = cmd.Date,
            Name = cmd.Name,
            Type = cmd.Type,
            CreatedAt = now,
            UpdatedAt = now
        }, ct);

        // Story 6.6: flag stale OT approvals for this date within the same transaction
        await FlagStaleOtApprovalsAsync(cmd.Date, now, ct);

        await _repo.SaveAsync(ct);
    }

    private async Task FlagStaleOtApprovalsAsync(DateOnly date, DateTimeOffset now, CancellationToken ct)
    {
        var stale = await _approvalQueue.GetOtApprovalsByDateAsync(date, ct);
        foreach (var ot in stale)
        {
            ot.IsStale = true;
            ot.UpdatedAt = now;
        }
    }
}
