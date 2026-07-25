using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DDFC.Infrastructure.Services;

public class PackageService : IPackageService
{
    private readonly DDFCDbContext _db;
    public PackageService(DDFCDbContext db) => _db = db;

    public Task<List<Package>> GetActivePackagesAsync(PlotType? plotType = null, PlotSize? plotSize = null, PackageCategory? category = null, DesignType? designType = null) =>
        _db.Packages
           .Include(p => p.LineItems)
           .Where(p => p.IsActive
               && (plotType   == null || p.PlotType         == plotType)
               && (plotSize   == null || p.PlotSize         == plotSize)
               && (category   == null || p.PackageCategory  == category)
               && (designType == null || p.DesignType       == designType))
           .OrderBy(p => p.PlotType).ThenBy(p => p.PlotSize).ThenBy(p => p.PackageTier)
           .ToListAsync();

    public Task<Package?> GetByIdAsync(Guid id) =>
        _db.Packages.Include(p => p.LineItems).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Package> UpdatePricingAsync(Guid id, List<PackageLineItemDto> lineItems)
    {
        var pkg = await _db.Packages.Include(p => p.LineItems).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Package {id} not found.");

        // Remove old items not in the new list
        var keepIds    = lineItems.Where(l => l.Id.HasValue).Select(l => l.Id!.Value).ToHashSet();
        var toRemove   = pkg.LineItems.Where(li => !keepIds.Contains(li.Id)).ToList();
        foreach (var r in toRemove) pkg.LineItems.Remove(r);

        foreach (var dto in lineItems)
        {
            if (dto.Id.HasValue)
            {
                var existing = pkg.LineItems.First(li => li.Id == dto.Id.Value);
                existing.ServiceName     = dto.ServiceName;
                existing.AmountDDFC      = dto.AmountDDFC;
                existing.AmountExclusive = dto.AmountExclusive;
                existing.IsFree          = dto.IsFree;
                existing.SortOrder       = dto.SortOrder;
            }
            else
            {
                pkg.LineItems.Add(new PackageLineItem
                {
                    PackageId        = pkg.Id,
                    ServiceName      = dto.ServiceName,
                    AmountDDFC       = dto.AmountDDFC,
                    AmountExclusive  = dto.AmountExclusive,
                    IsFree           = dto.IsFree,
                    SortOrder        = dto.SortOrder
                });
            }
        }

        _db.Packages.Update(pkg);
        await _db.SaveChangesAsync();
        return pkg;
    }

    public async Task<Package> CreatePackageAsync(CreatePackageDto dto)
    {
        var pkg = new Package
        {
            PlotType    = dto.PlotType,
            PlotSize    = dto.PlotSize,
            PackageTier = dto.PackageTier,
            IsActive    = true
        };
        foreach (var li in dto.LineItems)
            pkg.LineItems.Add(new PackageLineItem
            {
                ServiceName     = li.ServiceName,
                AmountDDFC      = li.AmountDDFC,
                AmountExclusive = li.AmountExclusive,
                IsFree          = li.IsFree,
                SortOrder       = li.SortOrder
            });

        _db.Packages.Add(pkg);
        await _db.SaveChangesAsync();
        return pkg;
    }
}
