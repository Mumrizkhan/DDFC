using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DDFC.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration     _config;
    private readonly UserManager<User>  _userManager;
    private readonly RoleManager<Role>  _roleManager;

    public JwtService(IConfiguration config, UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        _config      = config;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Builds a JWT for a staff user. Roles are read from Identity's UserRoles table;
    /// the matching Role entity supplies the business-level Permissions JSON.
    /// Individual permission claims are added for authorization policy checks.
    /// </summary>
    public async Task<string> GenerateStaffTokenAsync(User user)
    {
        // Identity is the authoritative source for role assignments
        var roleNames = await _userManager.GetRolesAsync(user);
        var roleName  = roleNames.FirstOrDefault() ?? string.Empty;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email,          user.Email ?? string.Empty),
            new(ClaimTypes.Name,           user.FullName),
            new("userId",                  user.Id.ToString()),
            new("role",                    roleName),
            new("departmentId",            user.DepartmentId?.ToString() ?? string.Empty),
            new("userType",                "staff"),
        };

        // Add role permission claims if role exists
        if (!string.IsNullOrEmpty(roleName))
        {
            var roleEntity = await _roleManager.FindByNameAsync(roleName);
            if (roleEntity != null)
            {
                // Get role claims from the role
                var roleClaims = await _roleManager.GetClaimsAsync(roleEntity);
                
                // Add all permission claims to the JWT
                foreach (var claim in roleClaims)
                {
                    if (claim.Type == "permission" && !string.IsNullOrEmpty(claim.Value))
                    {
                        claims.Add(new Claim("permission", claim.Value));
                    }
                }
            }
        }

        return BuildToken(claims);
    }

    public string GenerateCustomerToken(Customer customer)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new("customerId",               customer.Id.ToString()),
            new("cnic",                    customer.CNIC),
            new(ClaimTypes.Name,           customer.FullName),
            new("userType",                "customer"),
        };

        return BuildToken(claims);
    }

    private string BuildToken(List<Claim> claims)
    {
        var key   = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "DDFCSecretKey2026!!DDFCSecretKey2026!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"]   ?? "DDFC",
            audience:           _config["Jwt:Audience"] ?? "DDFCUsers",
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
