using DDFC.Domain.Entities;

namespace DDFC.Application.Interfaces;

public interface IRoundRobinService
{
    /// <summary>Returns next available user in the department, updates the index, null if none available.</summary>
    Task<User?> GetNextAvailableUserAsync(Guid departmentId);
}
