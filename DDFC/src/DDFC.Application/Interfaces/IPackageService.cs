using DDFC.Domain.Entities;
using DDFC.Domain.Enums;

namespace DDFC.Application.Interfaces;

public interface IPackageService
{
    Task<List<Package>> GetActivePackagesAsync(PlotType? plotType = null, PlotSize? plotSize = null, PackageCategory? category = null, DesignType? designType = null);
    Task<Package?> GetByIdAsync(Guid id);
    Task<Package> UpdatePricingAsync(Guid id, List<PackageLineItemDto> lineItems);
    Task<Package> CreatePackageAsync(CreatePackageDto dto);
}

public record PackageLineItemDto(
    Guid?   Id,
    string  ServiceName,
    decimal AmountDDFC,
    decimal AmountExclusive,
    bool    IsFree,
    int     SortOrder);

public record CreatePackageDto(
    PlotType    PlotType,
    PlotSize    PlotSize,
    PackageTier PackageTier,
    List<PackageLineItemDto> LineItems);
