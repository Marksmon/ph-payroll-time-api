namespace PhPayrollTimeApi.Domain.Entities;

public class ShiftSchedule
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTimeOffset ScheduleStart { get; set; }
    public DateTimeOffset ScheduleEnd { get; set; }
    public ICollection<BreakWindow> BreakWindows { get; set; } = new List<BreakWindow>();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
