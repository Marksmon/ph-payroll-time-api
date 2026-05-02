using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class RestDayScheduleApprovalConfiguration : IEntityTypeConfiguration<RestDayScheduleApproval>
{
    public void Configure(EntityTypeBuilder<RestDayScheduleApproval> builder)
    {
        builder.ToTable("rest_day_schedule_approvals");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.EmployeeId).HasColumnName("employee_id");
        builder.Property(r => r.ShiftScheduleId).HasColumnName("shift_schedule_id");
        builder.Property(r => r.RestDayDate).HasColumnName("rest_day_date");
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(r => r.ApprovedBySubClaim).HasColumnName("approved_by_sub_claim").HasMaxLength(200);
        builder.Property(r => r.ApprovedAt).HasColumnName("approved_at");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
    }
}
