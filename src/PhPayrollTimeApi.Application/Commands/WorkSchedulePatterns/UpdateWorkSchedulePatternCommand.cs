using PhPayrollTimeApi.Application.Abstractions;
using PhPayrollTimeApi.Domain.Exceptions;
using PhPayrollTimeApi.Domain.Interfaces;

namespace PhPayrollTimeApi.Application.Commands.WorkSchedulePatterns;

public record UpdateWorkSchedulePatternCommand(
    Guid PatternId,
    Guid EmployeeId,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate);

public class UpdateWorkSchedulePatternCommandHandler : ICommandHandler<UpdateWorkSchedulePatternCommand>
{
    private readonly IWorkSchedulePatternRepository _repo;

    public UpdateWorkSchedulePatternCommandHandler(IWorkSchedulePatternRepository repo) => _repo = repo;

    public async Task HandleAsync(UpdateWorkSchedulePatternCommand cmd, CancellationToken ct)
    {
        var pattern = await _repo.GetByIdAsync(cmd.PatternId, ct)
            ?? throw new EntityNotFoundException("WorkSchedulePattern", cmd.PatternId);

        if (pattern.EmployeeId != cmd.EmployeeId)
            throw new EntityNotFoundException("WorkSchedulePattern", cmd.PatternId);

        if (cmd.ExpiryDate.HasValue && cmd.ExpiryDate.Value <= cmd.EffectiveDate)
            throw new DomainValidationException("ExpiryDate must be after EffectiveDate.");

        pattern.EffectiveDate = cmd.EffectiveDate;
        pattern.ExpiryDate = cmd.ExpiryDate;
        pattern.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.SaveAsync(ct);
    }
}
