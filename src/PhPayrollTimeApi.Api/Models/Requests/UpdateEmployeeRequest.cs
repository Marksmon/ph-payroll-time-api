using System.ComponentModel.DataAnnotations;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record UpdateEmployeeRequest(
    [Required] string FullName,
    [Required] string EmployeeNumber,
    [Required] string JwtSubjectClaim);
