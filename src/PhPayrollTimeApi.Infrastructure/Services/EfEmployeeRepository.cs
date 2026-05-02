using Microsoft.EntityFrameworkCore;
using PhPayrollTimeApi.Domain.Entities;
using PhPayrollTimeApi.Domain.Interfaces;
using PhPayrollTimeApi.Infrastructure.Persistence;

namespace PhPayrollTimeApi.Infrastructure.Services;

public class EfEmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EfEmployeeRepository(AppDbContext db) => _db = db;

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<bool> EmployeeNumberExistsAsync(string employeeNumber, Guid? excludeId, CancellationToken ct) =>
        _db.Employees.AnyAsync(e => e.EmployeeNumber == employeeNumber &&
                                    (excludeId == null || e.Id != excludeId), ct);

    public Task<bool> JwtSubjectClaimExistsAsync(string jwtSubjectClaim, Guid? excludeId, CancellationToken ct) =>
        _db.Employees.AnyAsync(e => e.JwtSubjectClaim == jwtSubjectClaim &&
                                    (excludeId == null || e.Id != excludeId), ct);

    public async Task AddAsync(Employee employee, CancellationToken ct) =>
        await _db.Employees.AddAsync(employee, ct);

    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
