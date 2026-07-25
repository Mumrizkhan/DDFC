using DDFC.Domain.Entities;

namespace DDFC.Application.Interfaces;

public interface IJwtService
{
    Task<string> GenerateStaffTokenAsync(User user);
    string GenerateCustomerToken(Customer customer);
}
