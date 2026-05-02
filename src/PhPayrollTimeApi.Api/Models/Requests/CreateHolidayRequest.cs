using System.ComponentModel.DataAnnotations;
using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record CreateHolidayRequest(
    [Required] DateOnly Date,
    [Required] string Name,
    [Required] HolidayType Type);
