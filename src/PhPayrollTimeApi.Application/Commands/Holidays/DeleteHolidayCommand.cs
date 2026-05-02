using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.Holidays;

public record DeleteHolidayCommand(DateOnly Date);

public class DeleteHolidayCommandHandler : ICommandHandler<DeleteHolidayCommand>
{
    private readonly IHolidayRepository _repo;

    public DeleteHolidayCommandHandler(IHolidayRepository repo) => _repo = repo;

    public async Task HandleAsync(DeleteHolidayCommand cmd, CancellationToken ct)
    {
        var entry = await _repo.GetByDateAsync(cmd.Date, ct)
            ?? throw new EntityNotFoundException($"No holiday entry found for {cmd.Date:yyyy-MM-dd}.");

        await _repo.DeleteAsync(entry, ct);
        await _repo.SaveAsync(ct);
    }
}
