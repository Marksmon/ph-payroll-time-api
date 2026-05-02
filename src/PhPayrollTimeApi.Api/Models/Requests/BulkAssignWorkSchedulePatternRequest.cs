using System.ComponentModel.DataAnnotations;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record BulkAssignWorkSchedulePatternRequest(
    [Required] List<Guid> EmployeeIds,
    [Required] List<int> RestDays,
    [Required] DateOnly EffectiveDate,
    DateOnly? ExpiryDate);
