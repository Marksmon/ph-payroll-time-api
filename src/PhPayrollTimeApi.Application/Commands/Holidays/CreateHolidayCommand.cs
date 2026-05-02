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

    public CreateHolidayCommandHandler(IHolidayRepository repo) => _repo = repo;

    public async Task HandleAsync(CreateHolidayCommand cmd, CancellationToken ct)
    {
        if (await _repo.DateExistsAsync(cmd.Date, ct))
            throw new ConflictException(
                $"A holiday entry for {cmd.Date:yyyy-MM-dd} already exists.");

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

        await _repo.SaveAsync(ct);
    }
}
