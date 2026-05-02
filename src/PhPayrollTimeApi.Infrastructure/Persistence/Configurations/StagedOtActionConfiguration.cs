using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class StagedOtActionConfiguration : IEntityTypeConfiguration<StagedOtAction>
{
    public void Configure(EntityTypeBuilder<StagedOtAction> builder)
    {
        builder.ToTable("staged_ot_actions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.OtApprovalId).HasColumnName("ot_approval_id");
        builder.Property(s => s.ActionType).HasColumnName("action_type").HasConversion<string>();
        builder.Property(s => s.SegmentClassification).HasColumnName("segment_classification").HasConversion<string>();
        builder.Property(s => s.AdjustedDurationMinutes).HasColumnName("adjusted_duration_minutes");
        builder.Property(s => s.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
    }
}
