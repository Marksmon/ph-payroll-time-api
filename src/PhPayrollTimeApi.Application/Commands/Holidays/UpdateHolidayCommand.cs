using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Enums;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Holidays;

public record UpdateHolidayCommand(DateOnly Date, string Name, HolidayType Type);

public class UpdateHolidayCommandHandler : ICommandHandler<UpdateHolidayCommand>
{
    private readonly IHolidayRepository _repo;

    public UpdateHolidayCommandHandler(IHolidayRepository repo) => _repo = repo;

    public async Task HandleAsync(UpdateHolidayCommand cmd, CancellationToken ct)
    {
        var entry = await _repo.GetByDateAsync(cmd.Date, ct)
            ?? throw new EntityNotFoundException($"No holiday entry found for {cmd.Date:yyyy-MM-dd}.");

        entry.Name = cmd.Name;
        entry.Type = cmd.Type;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.SaveAsync(ct);
    }
}
