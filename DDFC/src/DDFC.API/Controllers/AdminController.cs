using DDFC.Domain.Entities;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

/// <summary>Admin endpoints: users, departments, customers, reports.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly DDFCDbContext   _db;
    private readonly UserManager<User>  _userManager;
    private readonly RoleManager<Role>  _roleManager;

    public AdminController(
        DDFCDbContext      db,
        UserManager<User>  userManager,
        RoleManager<Role>  roleManager)
    {
        _db          = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // ── Dashboard ──────────────────────────────────────────────────────────────
    [HttpGet("dashboard")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Dashboard()
    {
        var today         = DateTime.UtcNow.Date;
        var tomorrowStart = today.AddDays(1);
        var firstOfMonth  = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalRequestsToday = await _db.PossessionRequests
            .CountAsync(r => r.CreatedAt >= today && r.CreatedAt < tomorrowStart);

        var activeRequests = await _db.PossessionRequests
            .CountAsync(r => r.Status != DDFC.Domain.Enums.PossessionRequestStatus.Rejected
                          && r.Status != DDFC.Domain.Enums.PossessionRequestStatus.Delivered);

        var pendingPayments = await _db.Payments
            .CountAsync(p => p.Status == DDFC.Domain.Enums.PaymentStatus.Pending);

        var deliveredThisMonth = await _db.PossessionRequests
            .CountAsync(r => r.Status == DDFC.Domain.Enums.PossessionRequestStatus.Delivered
                          && r.UpdatedAt >= firstOfMonth);

        var requestsByStatus = await _db.PossessionRequests
            .GroupBy(r => r.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        var departmentWorkload = await _db.TaskAssignments
            .Where(t => t.Status == DDFC.Domain.Enums.TaskAssignmentStatus.Pending
                     || t.Status == DDFC.Domain.Enums.TaskAssignmentStatus.InProgress)
            .Join(_db.Departments, t => t.DepartmentId, d => d.Id,
                  (t, d) => new { d.DepartmentName })
            .GroupBy(x => x.DepartmentName)
            .Select(g => new { departmentName = g.Key, pending = g.Count() })
            .ToListAsync();

        var recentActivity = await _db.RequestWorkflowHistories
            .Include(h => h.ActionByUser)
            .OrderByDescending(h => h.Timestamp)
            .Take(20)
            .Select(h => new
            {
                historyId  = h.Id,
                requestId  = h.RequestId,
                fromStatus = h.FromStatus,
                toStatus   = h.ToStatus,
                actionBy   = h.ActionByUserId,
                actorName  = h.ActionByUser != null ? h.ActionByUser.FullName : null,
                comments   = h.Comments,
                timestamp  = h.Timestamp,
            })
            .ToListAsync();

        return Ok(new
        {
            totalRequestsToday,
            activeRequests,
            pendingPayments,
            deliveredThisMonth,
            requestsByStatus,
            departmentWorkload,
            recentActivity,
            avgTurnaroundByStage = Array.Empty<object>(),
        });
    }

    // ── Users ──────────────────────────────────────────────────────────────────
    [HttpGet("users")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .Where(u => !u.IsDeleted)
            .Include(u => u.Department)
            .ToListAsync();

        var result = new List<StaffUserDto>();
        foreach (var u in users)
        {
            var roles    = await _userManager.GetRolesAsync(u);
            var roleName = roles.FirstOrDefault();
            var role     = roleName is not null ? await _roleManager.FindByNameAsync(roleName) : null;
            result.Add(MapToDto(u, role, roleName));
        }
        return Ok(result);
    }

    [HttpGet("users/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var u = await _userManager.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (u is null) return NotFound();

        var roles    = await _userManager.GetRolesAsync(u);
        var roleName = roles.FirstOrDefault();
        var role     = roleName is not null ? await _roleManager.FindByNameAsync(roleName) : null;
        return Ok(MapToDto(u, role, roleName));
    }

    [HttpPost("users")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var role = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
        if (role is null) return BadRequest(new { message = "Role not found." });

        var user = new User
        {
            UserName       = dto.Email,
            Email          = dto.Email,
            FullName       = dto.FullName,
            DepartmentId   = dto.DepartmentId,
            IsActive       = true,
            EmailConfirmed = true,
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, role.Name!);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            MapToDto(user, role, role.Name));
    }

    [HttpPut("users/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u is null || u.IsDeleted) return NotFound();

        u.FullName     = dto.FullName     ?? u.FullName;
        u.DepartmentId = dto.DepartmentId ?? u.DepartmentId;
        u.IsActive     = dto.IsActive     ?? u.IsActive;

        var updateResult = await _userManager.UpdateAsync(u);
        if (!updateResult.Succeeded)
            return BadRequest(new { errors = updateResult.Errors.Select(e => e.Description) });

        // Role change
        if (dto.RoleId.HasValue)
        {
            var newRole = await _roleManager.FindByIdAsync(dto.RoleId.Value.ToString());
            if (newRole is null) return BadRequest(new { message = "Role not found." });

            var currentRoles = await _userManager.GetRolesAsync(u);
            await _userManager.RemoveFromRolesAsync(u, currentRoles);
            await _userManager.AddToRoleAsync(u, newRole.Name!);
        }

        // Password change
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var token  = await _userManager.GeneratePasswordResetTokenAsync(u);
            var pwResult = await _userManager.ResetPasswordAsync(u, token, dto.NewPassword);
            if (!pwResult.Succeeded)
                return BadRequest(new { errors = pwResult.Errors.Select(e => e.Description) });
        }

        var roles    = await _userManager.GetRolesAsync(u);
        var roleName = roles.FirstOrDefault();
        var role     = roleName is not null ? await _roleManager.FindByNameAsync(roleName) : null;
        return Ok(MapToDto(u, role, roleName));
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u is null) return NotFound();
        u.IsDeleted = true;
        u.IsActive  = false;
        await _userManager.UpdateAsync(u);
        return NoContent();
    }

    // ── Customers ──────────────────────────────────────────────────────────────
    [HttpGet("customers")]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> GetCustomers() =>
        Ok(await _db.Customers
            .Where(c => !c.IsDeleted)
            .Select(c => new CustomerDto(
                c.Id.ToString(),
                c.FullName,
                c.CNIC,
                c.PhoneNumber,
                c.Email,
                c.Address,
                c.CreatedAt.ToString("o")))
            .ToListAsync());

    [HttpGet("customers/search")]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> SearchCustomers([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(await _db.Customers
                .Where(c => !c.IsDeleted)
                .Select(c => new CustomerDto(
                    c.Id.ToString(),
                    c.FullName,
                    c.CNIC,
                    c.PhoneNumber,
                    c.Email,
                    c.Address,
                    c.CreatedAt.ToString("o")))
                .ToListAsync());

        var searchLower = query.ToLower();
        return Ok(await _db.Customers
            .Where(c => !c.IsDeleted && (
                c.CNIC.ToLower().Contains(searchLower) ||
                c.PhoneNumber.ToLower().Contains(searchLower) ||
                c.FullName.ToLower().Contains(searchLower)))
            .Select(c => new CustomerDto(
                c.Id.ToString(),
                c.FullName,
                c.CNIC,
                c.PhoneNumber,
                c.Email,
                c.Address,
                c.CreatedAt.ToString("o")))
            .ToListAsync());
    }

    [HttpGet("customers/{id:guid}")]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var c = await _db.Customers
            .Where(x => !x.IsDeleted && x.Id == id)
            .FirstOrDefaultAsync();
        return c is null ? NotFound() : Ok(new CustomerDto(
            c.Id.ToString(),
            c.FullName,
            c.CNIC,
            c.PhoneNumber,
            c.Email,
            c.Address,
            c.CreatedAt.ToString("o")));
    }

    [HttpPost("customers")]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Full name is required." });
        
        if (string.IsNullOrWhiteSpace(dto.Cnic))
            return BadRequest(new { message = "CNIC is required." });
        
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return BadRequest(new { message = "Phone number is required." });

        // Check if CNIC already exists
        if (await _db.Customers.AnyAsync(c => !c.IsDeleted && c.CNIC == dto.Cnic))
            return Conflict(new { message = "CNIC already registered." });

        // Check if phone number already exists
        if (await _db.Customers.AnyAsync(c => !c.IsDeleted && c.PhoneNumber == dto.PhoneNumber))
            return Conflict(new { message = "Phone number already registered." });

        var customer = new Customer
        {
            FullName             = dto.FullName.Trim(),
            CNIC                 = dto.Cnic.Trim(),
            PhoneNumber          = dto.PhoneNumber.Trim(),
            Email                = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            IsActive             = true,
            PreferredSmsLanguage = "EN"
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        
        var customerDto = new CustomerDto(
            customer.Id.ToString(),
            customer.FullName,
            customer.CNIC,
            customer.PhoneNumber,
            customer.Email,
            customer.Address,
            customer.CreatedAt.ToString("o"));
        
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customerDto);
    }

    [HttpGet("customers/{customerId:guid}/plots")]
    [Authorize(Policy = "CanManageCustomers")]
    public async Task<IActionResult> GetCustomerPlots(Guid customerId)
    {
        var customer = await _db.Customers
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .FirstOrDefaultAsync();
        
        if (customer is null)
            return NotFound(new { message = "Customer not found." });

        var plots = await _db.PossessionRequests
            .Where(r => r.CustomerId == customerId && !r.IsDeleted)
            .Include(r => r.Plot)
            .Select(r => r.Plot!)
            .Distinct()
            .Select(p => new
            {
                plotId = p.Id.ToString(),
                p.PlotNumber,
                p.SectorNo,
                p.StreetNo,
                p.PhaseNo,
                p.PlotSize,
                p.PlotType,
                p.CurrentStatus
            })
            .ToListAsync();

        return Ok(plots);
    }

    // ── Departments ────────────────────────────────────────────────────────────
    [HttpGet("departments")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetDepartments() =>
        Ok(await _db.Departments.Include(d => d.Users).ToListAsync());

    [HttpPost("departments")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDeptDto dto)
    {
        var dept = new Department { DepartmentName = dto.Name, DepartmentCode = dto.Code };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();
        return Ok(dept);
    }

    [HttpPut("departments/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDeptDto dto)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept is null) return NotFound();
        if (dto.Name is not null) dept.DepartmentName = dto.Name;
        if (dto.Code is not null) dept.DepartmentCode = dto.Code;
        await _db.SaveChangesAsync();
        return Ok(dept);
    }

    // ── Roles ──────────────────────────────────────────────────────────────────
    [HttpGet("roles")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetRoles() =>
        Ok(await _roleManager.Roles.ToListAsync());

    [HttpPut("roles/{id:guid}/permissions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdatePermissionsDto dto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null) return NotFound();
        if (role.IsBuiltIn) return BadRequest(new { message = "Cannot modify built-in role permissions." });
        role.Permissions = dto.Permissions;
        await _roleManager.UpdateAsync(role);
        return Ok(role);
    }

    // ── Plots ──────────────────────────────────────────────────────────────────
    [HttpGet("plots/phases")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlotPhases() =>
        Ok(await _db.Plots
            .Select(p => p.PhaseNo)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync());

    [HttpGet("plots/sectors")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlotSectors([FromQuery] string? phaseNo) =>
        Ok(await _db.Plots
            .Where(p => phaseNo == null || p.PhaseNo == phaseNo)
            .Select(p => p.SectorNo)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync());

    [HttpGet("plots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlots(
        [FromQuery] DDFC.Domain.Enums.PlotStatus? status,
        [FromQuery] string? sectorNo,
        [FromQuery] string? phaseNo,
        [FromQuery] DDFC.Domain.Enums.PlotType? plotType) =>
        Ok(await _db.Plots
            .Where(p => (status == null || p.CurrentStatus == status)
                     && (sectorNo == null || p.SectorNo == sectorNo)
                     && (phaseNo == null || p.PhaseNo == phaseNo)
                     && (plotType == null || p.PlotType == plotType))
            .ToListAsync());

    [HttpPost("plots")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreatePlot([FromBody] CreatePlotDto dto)
    {
        var plot = new Plot
        {
            PlotNumber    = dto.PlotNumber,
            SectorNo      = dto.SectorNo,
            StreetNo      = dto.StreetNo,
            PhaseNo       = dto.PhaseNo ?? string.Empty,
            PlotSize      = dto.PlotSize,
            PlotType      = dto.PlotType,
            CurrentStatus = DDFC.Domain.Enums.PlotStatus.Available
        };
        _db.Plots.Add(plot);
        await _db.SaveChangesAsync();
        return Ok(plot);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private static StaffUserDto MapToDto(User u, Role? role, string? roleName) =>
        new(u.Id, u.FullName, u.Email!, roleName, role?.Id,
            u.DepartmentId, u.Department?.DepartmentName,
            u.IsActive, u.IsAvailable, u.LastLogin, u.CreatedAt);
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record CreateUserDto(string FullName, string Email, string Password, Guid RoleId, Guid DepartmentId);
public record UpdateUserDto(string? FullName, Guid? RoleId, Guid? DepartmentId, bool? IsActive, string? NewPassword);
public record CustomerDto(string CustomerId, string FullName, string Cnic, string PhoneNumber, string? Email, string? Address, string CreatedAt);
public record CreateCustomerDto(string FullName, string Cnic, string PhoneNumber, string? Email);
public record CreateDeptDto(string Name, string Code);
public record UpdateDeptDto(string? Name, string? Code);
public record UpdatePermissionsDto(string Permissions);
public record CreatePlotDto(
    string PlotNumber, string SectorNo, string? StreetNo, string? PhaseNo,
    DDFC.Domain.Enums.PlotSize PlotSize, DDFC.Domain.Enums.PlotType PlotType);
public record StaffUserDto(
    Guid    UserId,
    string  FullName,
    string  Email,
    string? RoleName,
    Guid?   RoleId,
    Guid?   DepartmentId,
    string? DepartmentName,
    bool    IsActive,
    bool    IsAvailable,
    DateTime? LastLogin,
    DateTime  CreatedAt);
