using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence.Configurations;

public class HolidayCalendarEntryConfiguration : IEntityTypeConfiguration<HolidayCalendarEntry>
{
    public void Configure(EntityTypeBuilder<HolidayCalendarEntry> builder)
    {
        builder.ToTable("holiday_calendar_entries");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.Date).HasColumnName("date");
        builder.Property(h => h.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(h => h.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(h => h.CreatedAt).HasColumnName("created_at");
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(h => h.Date).IsUnique();
    }
}
