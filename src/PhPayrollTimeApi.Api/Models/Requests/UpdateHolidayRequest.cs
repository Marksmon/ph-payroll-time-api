using System.ComponentModel.DataAnnotations;
using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record UpdateHolidayRequest(
    [Required] string Name,
    [Required] HolidayType Type);
