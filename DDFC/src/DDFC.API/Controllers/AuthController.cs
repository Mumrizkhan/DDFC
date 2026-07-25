using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IJwtService       _jwt;
    private readonly IOtpService       _otp;
    private readonly DDFCDbContext     _db;

    public AuthController(UserManager<User> userManager, RoleManager<Role> roleManager, IJwtService jwt, IOtpService otp, DDFCDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwt  = jwt;
        _otp  = otp;
        _db   = db;
    }

    [HttpPost("staff/login")]
    public async Task<IActionResult> StaffLogin([FromBody] StaffLoginRequest req)
    {
        // Identity validates credentials — no BCrypt dependency
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive || user.IsDeleted)
            return Unauthorized(new { message = "Invalid credentials." });

        if (!await _userManager.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        // Track last login
        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = await _jwt.GenerateStaffTokenAsync(user);

        // Fetch role info for the login response (Identity stores roles separately)
        var roleNames = await _userManager.GetRolesAsync(user);
        var roleName = roleNames.FirstOrDefault();
        var dept = user.DepartmentId.HasValue
            ? await _db.Departments.FindAsync(user.DepartmentId.Value)
            : null;

        // Fetch role entity
        var roleEntity = roleName != null
            ? await _roleManager.FindByNameAsync(roleName)
            : null;

        // Extract permissions from role claims
        var permissionList = new List<string>();
        if (roleEntity != null)
        {
            var roleClaims = await _roleManager.GetClaimsAsync(roleEntity);
            permissionList = roleClaims
                .Where(c => c.Type == "permission" && !string.IsNullOrEmpty(c.Value))
                .Select(c => c.Value)
                .ToList();
        }

        // Serialize permissions as JSON array
        var permissions = System.Text.Json.JsonSerializer.Serialize(permissionList);

        return Ok(new
        {
            token,
            user = new
            {
                userId = user.Id,
                user.FullName,
                user.Email,
                roleName,
                roleId = roleEntity?.Id,
                departmentId = user.DepartmentId,
                departmentName = dept?.DepartmentName,
                permissions
            }
        });
    }

    // ── Customer ──────────────────────────────────────────────────────────────
    [HttpPost("customer/request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequest req)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CNIC == req.Cnic && c.IsActive && !c.IsDeleted);

        if (customer is null)
            return NotFound(new { message = "No active customer found for this CNIC." });

        var otp = await _otp.GenerateOtpAsync(req.Cnic);
        return Ok(new { message = "OTP sent.", otpDev = otp }); // Remove otpDev in prod
    }

    [HttpPost("customer/verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerify req)
    {
        var valid = await _otp.ValidateOtpAsync(req.Cnic, req.Otp);
        if (!valid) return Unauthorized(new { message = "Invalid or expired OTP." });

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CNIC == req.Cnic && c.IsActive);

        if (customer is null) return NotFound();

        var token = _jwt.GenerateCustomerToken(customer);
        return Ok(new { token, customerId = customer.Id, fullName = customer.FullName });
    }
}

public record StaffLoginRequest(string Email, string Password);
public record OtpRequest(string Cnic);
public record OtpVerify(string Cnic, string Otp);
