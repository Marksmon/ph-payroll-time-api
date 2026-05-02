using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class WorkSchedulePatternConfiguration : IEntityTypeConfiguration<WorkSchedulePattern>
{
    public void Configure(EntityTypeBuilder<WorkSchedulePattern> builder)
    {
        builder.ToTable("work_schedule_patterns");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.EmployeeId).HasColumnName("employee_id");
        builder.Property(w => w.EffectiveDate).HasColumnName("effective_date");
        builder.Property(w => w.ExpiryDate).HasColumnName("expiry_date");
        builder.Property(w => w.IsActive).HasColumnName("is_active");
        builder.Property(w => w.CreatedAt).HasColumnName("created_at");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at");

        // Store ICollection<DayOfWeek> as PostgreSQL integer[]
        builder.Property(w => w.RestDays)
            .HasColumnName("rest_days")
            .HasColumnType("integer[]")
            .HasConversion(
                v => v.Select(d => (int)d).ToArray(),
                v => (ICollection<DayOfWeek>)v.Select(i => (DayOfWeek)i).ToList());

        builder.HasOne(w => w.Employee)
            .WithMany()
            .HasForeignKey(w => w.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
