using System.ComponentModel.DataAnnotations;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record AssignWorkSchedulePatternRequest(
    [Required] List<int> RestDays,
    [Required] DateOnly EffectiveDate,
    DateOnly? ExpiryDate);
