using PhPayrollTimeApi.Domain.Enums;

namespace PhPayrollTimeApi.Domain.Entities;

public class HolidayCalendarEntry
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public HolidayType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
