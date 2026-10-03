using DDFC.Domain.Entities;
using DDFC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/templates")]
public class TemplatesController : ControllerBase
{
    private readonly DDFCDbContext _db;
    public TemplatesController(DDFCDbContext db) => _db = db;

    // ── Get All ───────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _db.Templates
            .Where(t => t.IsActive)
            .OrderBy(t => t.TemplateName)
            .ToListAsync();
        return Ok(templates);
    }

    // ── Get By ID ─────────────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null || !template.IsActive) return NotFound();
        return Ok(template);
    }

    // ── Update Content (Admin only) ───────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateDto dto)
    {
        var template = await _db.Templates.FindAsync(id);
        if (template is null || !template.IsActive) return NotFound();

        if (dto.Content is not null)
            template.Content = dto.Content;

        if (dto.TemplateName is not null)
            template.TemplateName = dto.TemplateName;

        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = Guid.TryParse(User.FindFirst("userId")?.Value, out var uid) ? uid : null;

        _db.Templates.Update(template);
        await _db.SaveChangesAsync();
        return Ok(template);
    }
}

public class UpdateTemplateDto
{
    public string? Content { get; set; }
    public string? TemplateName { get; set; }
}
