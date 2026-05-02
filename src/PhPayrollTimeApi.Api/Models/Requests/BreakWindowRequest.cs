using System.ComponentModel.DataAnnotations;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record BreakWindowRequest(
    [Required] DateTimeOffset BreakStart,
    [Required] DateTimeOffset BreakEnd);
