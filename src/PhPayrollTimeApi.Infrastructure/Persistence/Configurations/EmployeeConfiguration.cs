using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EmployeeNumber).HasColumnName("employee_number").HasMaxLength(50).IsRequired();
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Role).HasColumnName("role").HasConversion<string>();
        builder.Property(e => e.JwtSubjectClaim).HasColumnName("jwt_subject_claim").HasMaxLength(200).IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(e => e.EmployeeNumber).IsUnique();
        builder.HasIndex(e => e.JwtSubjectClaim).IsUnique();
    }
}
