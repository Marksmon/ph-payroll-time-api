using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(f => f.IsEnabled).HasColumnName("is_enabled");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");
        builder.Property(f => f.UpdatedBySubClaim).HasColumnName("updated_by_sub_claim").HasMaxLength(200);
        builder.HasIndex(f => f.Name).IsUnique();
    }
}
