using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DDFC.Infrastructure.Services;

public class RoundRobinService : IRoundRobinService
{
    private readonly DDFCDbContext _db;

    public RoundRobinService(DDFCDbContext db) => _db = db;

    public async Task<User?> GetNextAvailableUserAsync(Guid departmentId)
    {
        var dept = await _db.Departments.FindAsync(departmentId);
        if (dept is null) return null;

        var users = await _db.Users
            .Where(u => u.DepartmentId == departmentId && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        if (users.Count == 0) return null;

        // Check availability schedule
        var now = DateTime.UtcNow;
        var unavailableIds = await _db.EmployeeAvailabilities
            .Where(e => e.DepartmentId == departmentId
                     && !e.IsAvailable
                     && (e.UnavailableUntil == null || e.UnavailableUntil > now))
            .Select(e => e.UserId)
            .ToListAsync();

        var available = users.Where(u => u.IsAvailable && !unavailableIds.Contains(u.Id)).ToList();
        if (available.Count == 0) return null;

        // Round-robin: pick the user at the current index (mod count), then increment
        int idx = dept.LastAssignedEmployeeIndex % available.Count;
        var selected = available[idx];

        dept.LastAssignedEmployeeIndex = (idx + 1) % available.Count;
        _db.Departments.Update(dept);
        await _db.SaveChangesAsync();

        return selected;
    }
}
