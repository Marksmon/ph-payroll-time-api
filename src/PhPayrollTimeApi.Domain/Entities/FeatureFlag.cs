namespace PhPayrollTimeApi.Domain.Entities;

public class FeatureFlag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedBySubClaim { get; set; }
}
