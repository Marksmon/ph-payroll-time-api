using System.ComponentModel.DataAnnotations;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record UpdateWorkSchedulePatternRequest(
    [Required] DateOnly EffectiveDate,
    DateOnly? ExpiryDate);
