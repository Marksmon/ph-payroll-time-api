using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class OtApprovalConfiguration : IEntityTypeConfiguration<OtApproval>
{
    public void Configure(EntityTypeBuilder<OtApproval> builder)
    {
        builder.ToTable("ot_approvals");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.EmployeeId).HasColumnName("employee_id");
        builder.Property(o => o.ShiftScheduleId).HasColumnName("shift_schedule_id");
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(o => o.CommittedBySubClaim).HasColumnName("committed_by_sub_claim").HasMaxLength(200);
        builder.Property(o => o.CommittedAt).HasColumnName("committed_at");
        builder.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(500);
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(o => o.StagedActions)
            .WithOne(s => s.OtApproval)
            .HasForeignKey(s => s.OtApprovalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
