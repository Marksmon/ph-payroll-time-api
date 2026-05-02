using System.ComponentModel.DataAnnotations;
using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record CreateEmployeeRequest(
    [Required] string EmployeeNumber,
    [Required] string FullName,
    [Required] UserRole Role,
    [Required] string JwtSubjectClaim);
