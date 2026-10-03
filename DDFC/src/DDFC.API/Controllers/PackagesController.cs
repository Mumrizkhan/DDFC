using DDFC.Application.Interfaces;
using DDFC.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DDFC.API.Controllers;

[ApiController]
[Route("api/v1/packages")]
[Authorize]
public class PackagesController : ControllerBase
{
    private readonly IPackageService _svc;
    public PackagesController(IPackageService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PlotType? plotType, [FromQuery] PlotSize? plotSize, [FromQuery] PackageCategory? category, [FromQuery] DesignType? designType)
        => Ok(await _svc.GetActivePackagesAsync(plotType, plotSize, category, designType));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var pkg = await _svc.GetByIdAsync(id);
        return pkg is null ? NotFound() : Ok(pkg);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePackageDto dto)
    {
        var pkg = await _svc.CreatePackageAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = pkg.Id }, pkg);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePricing(Guid id, [FromBody] UpdatePackageDto dto)
    {
        try
        {
            var pkg = await _svc.UpdatePricingAsync(id, dto.LineItems);
            return Ok(pkg);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

public class UpdatePackageDto
{
    public List<PackageLineItemDto> LineItems { get; set; } = new();
}
