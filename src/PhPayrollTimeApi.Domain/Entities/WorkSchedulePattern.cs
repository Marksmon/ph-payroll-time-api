namespace PhPayrollTimeApi.Domain.Entities;

public class WorkSchedulePattern
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<DayOfWeek> RestDays { get; set; } = new List<DayOfWeek>();
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
