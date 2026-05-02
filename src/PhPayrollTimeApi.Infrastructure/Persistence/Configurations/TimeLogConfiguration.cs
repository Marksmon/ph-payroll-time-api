using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class TimeLogConfiguration : IEntityTypeConfiguration<TimeLog>
{
    public void Configure(EntityTypeBuilder<TimeLog> builder)
    {
        builder.ToTable("time_logs");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.EmployeeId).HasColumnName("employee_id");
        builder.Property(t => t.LogType).HasColumnName("log_type").HasConversion<string>();
        builder.Property(t => t.LoggedAt).HasColumnName("logged_at");
        builder.Property(t => t.Source).HasColumnName("source").HasMaxLength(100);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");

        builder.HasOne(t => t.Employee)
            .WithMany()
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.EmployeeId, t.LoggedAt });
    }
}
