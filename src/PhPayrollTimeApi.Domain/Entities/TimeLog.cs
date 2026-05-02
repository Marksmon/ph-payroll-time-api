using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.Entities;

public class TimeLog
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public LogType LogType { get; set; }
    public DateTimeOffset LoggedAt { get; set; }
    public string? Source { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
