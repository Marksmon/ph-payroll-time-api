using PhPayrollTimeApi.Application.Abstractions;

namespace PhPayrollTimeApi.Api.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register all ICommandHandler<T> and IQueryHandler<T,R> implementations from Application assembly
        var applicationAssembly = typeof(ICommandHandler<>).Assembly;

        foreach (var type in applicationAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                var def = iface.GetGenericTypeDefinition();
                if (def == typeof(ICommandHandler<>) || def == typeof(IQueryHandler<,>))
                    services.AddScoped(iface, type);
            }
        }

        return services;
    }
}
