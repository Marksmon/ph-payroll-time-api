using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.ValueObjects;
using Xunit;

namespace PhPayrollTimeApi.Domain.Tests;

public class EntityDateTimeEnforcementTests
{
    [Fact]
    public void AllDomainEntities_HaveNoDateTimeProperties()
    {
        var domainAssembly = typeof(Employee).Assembly;

        var targetTypes = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        (t.Namespace?.Contains(".Entities") == true ||
                         t.Namespace?.Contains(".ValueObjects") == true));

        var violations = targetTypes
            .SelectMany(t => t.GetProperties())
            .Where(p => p.PropertyType == typeof(DateTime) ||
                        p.PropertyType == typeof(DateTime?))
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
            .ToList();

        Assert.True(violations.Count == 0,
            $"DateTime properties found — use DateTimeOffset instead: {string.Join(", ", violations)}");
    }
}
