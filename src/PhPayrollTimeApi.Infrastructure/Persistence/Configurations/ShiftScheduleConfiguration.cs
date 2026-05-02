using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class ShiftScheduleConfiguration : IEntityTypeConfiguration<ShiftSchedule>
{
    public void Configure(EntityTypeBuilder<ShiftSchedule> builder)
    {
        builder.ToTable("shift_schedules");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.EmployeeId).HasColumnName("employee_id");
        builder.Property(s => s.ScheduleStart).HasColumnName("schedule_start");
        builder.Property(s => s.ScheduleEnd).HasColumnName("schedule_end");
        builder.Property(s => s.IsActive).HasColumnName("is_active");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsMany(s => s.BreakWindows, bw =>
        {
            bw.ToTable("shift_schedule_break_windows");
            bw.WithOwner().HasForeignKey("shift_schedule_id");
            bw.Property(b => b.BreakStart).HasColumnName("break_start");
            bw.Property(b => b.BreakEnd).HasColumnName("break_end");
        });

        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
