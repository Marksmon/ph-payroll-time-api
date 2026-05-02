using System.ComponentModel.DataAnnotations;
using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record SubmitTimeLogRequest(
    [Required] LogType LogType,
    DateTimeOffset? LoggedAt,
    string? Source);
