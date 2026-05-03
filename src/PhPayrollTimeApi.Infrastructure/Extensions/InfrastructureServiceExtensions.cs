using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Domain.Services;
using PhPayrollTimeApi.Infrastructure.Persistence;
using PhPayrollTimeApi.Infrastructure.Services;

namespace PhPayrollTimeApi.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEmployeeRepository, EfEmployeeRepository>();
        services.AddScoped<IShiftScheduleRepository, EfShiftScheduleRepository>();
        services.AddScoped<IWorkSchedulePatternRepository, EfWorkSchedulePatternRepository>();
        services.AddScoped<IHolidayRepository, EfHolidayRepository>();
        services.AddScoped<ITimeLogRepository, EfTimeLogRepository>();

        services.AddSingleton<IClockProvider, SystemClockProvider>();
        // Epics 1-7: HardcodedFeatureFlagProvider (all flags true). Epic 8 Story 8.2 switches to DbFeatureFlagProvider.
        services.AddSingleton<IFeatureFlagProvider, HardcodedFeatureFlagProvider>();
        services.AddScoped<IHolidayCalendar, EfHolidayCalendar>();
        services.AddScoped<IHolidayApprovalRepository, EfHolidayApprovalRepository>();
        services.AddScoped<IApprovalQueueRepository, EfApprovalQueueRepository>();
        // Scoped: fresh per request to isolate log claim state during computation
        services.AddScoped<ILogClaimTracker, InMemoryLogClaimTracker>();

        // Domain service — registered scoped so it receives scoped dependencies (ILogClaimTracker, IHolidayCalendar)
        services.AddScoped<ComputationEngine>();

        return services;
    }
}
