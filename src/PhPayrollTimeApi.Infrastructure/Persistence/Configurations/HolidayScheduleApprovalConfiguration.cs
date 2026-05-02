using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class HolidayScheduleApprovalConfiguration : IEntityTypeConfiguration<HolidayScheduleApproval>
{
    public void Configure(EntityTypeBuilder<HolidayScheduleApproval> builder)
    {
        builder.ToTable("holiday_schedule_approvals");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.EmployeeId).HasColumnName("employee_id");
        builder.Property(h => h.ShiftScheduleId).HasColumnName("shift_schedule_id");
        builder.Property(h => h.HolidayDate).HasColumnName("holiday_date");
        builder.Property(h => h.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(h => h.ApprovedBySubClaim).HasColumnName("approved_by_sub_claim").HasMaxLength(200);
        builder.Property(h => h.ApprovedAt).HasColumnName("approved_at");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at");
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at");
    }
}
