using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;

namespace PhPayrollTimeApi.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<WorkSchedulePattern> WorkSchedulePatterns => Set<WorkSchedulePattern>();
    public DbSet<TimeLog> TimeLogs => Set<TimeLog>();
    public DbSet<HolidayCalendarEntry> HolidayCalendarEntries => Set<HolidayCalendarEntry>();
    public DbSet<HolidayScheduleApproval> HolidayScheduleApprovals => Set<HolidayScheduleApproval>();
    public DbSet<RestDayScheduleApproval> RestDayScheduleApprovals => Set<RestDayScheduleApproval>();
    public DbSet<OtApproval> OtApprovals => Set<OtApproval>();
    public DbSet<StagedOtAction> StagedOtActions => Set<StagedOtAction>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global convention: all DateTimeOffset properties → PostgreSQL timestamptz
        modelBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        modelBuilder.Properties<DateTimeOffset?>().HaveColumnType("timestamptz");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
