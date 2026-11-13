using System.ComponentModel.DataAnnotations;
using DDFC.Domain.Entities;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/laboratories")]
[Authorize(Policy = "StaffOrAdmin")]
public class LaboratoriesController(DDFCDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        if (includeInactive && !User.HasClaim("role", "Admin")) return Forbid();
        var labs = await db.Laboratories.Where(lab => includeInactive || lab.IsActive)
            .OrderBy(lab => lab.Name)
            .Select(lab => new LaboratoryResponse(lab.Id, lab.Name, lab.Email, lab.Phone, lab.IsActive))
            .ToListAsync();
        return Ok(labs);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] SaveLaboratoryRequest input)
    {
        var name = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Lab name is required." });
        if (await db.Laboratories.AnyAsync(lab => lab.Name == name))
            return Conflict(new { message = "A lab with this name already exists." });
        var lab = new Laboratory { Name = name, Email = input.Email?.Trim(), Phone = input.Phone?.Trim(), IsActive = input.IsActive };
        db.Laboratories.Add(lab);
        await db.SaveChangesAsync();
        return StatusCode(201, new LaboratoryResponse(lab.Id, lab.Name, lab.Email, lab.Phone, lab.IsActive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveLaboratoryRequest input)
    {
        var lab = await db.Laboratories.FirstOrDefaultAsync(item => item.Id == id);
        if (lab == null) return NotFound();
        var name = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Lab name is required." });
        if (await db.Laboratories.AnyAsync(item => item.Id != id && item.Name == name))
            return Conflict(new { message = "A lab with this name already exists." });
        lab.Name = name;
        lab.Email = input.Email?.Trim();
        lab.Phone = input.Phone?.Trim();
        lab.IsActive = input.IsActive;
        await db.SaveChangesAsync();
        return Ok(new LaboratoryResponse(lab.Id, lab.Name, lab.Email, lab.Phone, lab.IsActive));
    }
}

/// <summary>Lab configuration maintained by system administrators.</summary>
public sealed record SaveLaboratoryRequest
{
    [Required, MaxLength(200)] public required string Name { get; init; }
    [EmailAddress, MaxLength(320)] public string? Email { get; init; }
    [MaxLength(50)] public string? Phone { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>Lab available for soil-test scheduling.</summary>
public sealed record LaboratoryResponse(Guid Id, string Name, string? Email, string? Phone, bool IsActive);