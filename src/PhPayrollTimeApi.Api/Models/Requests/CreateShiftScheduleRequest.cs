using System.ComponentModel.DataAnnotations;

namespace PhPayrollTimeApi.Api.Models.Requests;

public record CreateShiftScheduleRequest(
    [Required] DateTimeOffset ScheduleStart,
    [Required] DateTimeOffset ScheduleEnd,
    List<BreakWindowRequest>? BreakWindows);
