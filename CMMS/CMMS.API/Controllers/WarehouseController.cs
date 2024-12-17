using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/warehouse")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {

        private readonly IWarehouseService _warehouseService;
        private readonly IVariantService _variantService;
        private readonly IConversionUnitService _conversionUnitService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        public WarehouseController(IWarehouseService warehouseService, IVariantService variantService, IConversionUnitService conversionUnitService, IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _warehouseService = warehouseService;
            _conversionUnitService = conversionUnitService;
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;
        }
        [HttpGet("get-warehouse-products")]
        public async Task<IActionResult> Get([FromQuery] int? quantityStatus, [FromQuery] string? materialName, [FromQuery] int? page, [FromQuery] int? itemPerPage,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId)
        {
            try
            {
                // await BalanceQuantity();

                var items = await _warehouseService
                     .Get(x => (materialName.IsNullOrEmpty() || x.Material.Name.ToLower().Contains(materialName.ToLower())) &&
                               (categoryId == null || x.Material.CategoryId == categoryId) && (brandId == null || x.Material.BrandId == brandId)).
                     Include(x => x.Material).
                     Include(x => x.Variant).ThenInclude(x => x.MaterialVariantAttributes).ThenInclude(x => x.Attribute).
                     Include(x => x.Variant).ThenInclude(x => x.ConversionUnit).Select(x => new WarehouseDTO()
                     {
                         Id = x.Id,
                         MaterialId = x.MaterialId,
                         MaterialCode = x.Material.MaterialCode,
                         MaterialName = x.Material.Name,
                         MaterialImage = x.Material.ImageUrl,
                         MaterialPrice = x.Material.SalePrice,
                         MaterialCostPrice = x.Material.CostPrice,
                         VariantId = x.VariantId,
                         VariantName = x.Variant == null ? null : x.Variant.SKU,
                         VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
                         Quantity = x.InRequestQuantity == null ? x.TotalQuantity : x.TotalQuantity - (decimal)x.InRequestQuantity,
                         MinStock = x.Material.MinStock,
                         MaxStock = x.Material.MaxStock,
                         InOrderQuantity = x.InRequestQuantity,
                         VariantPrice = x.Variant == null ? null : x.Variant.Price,
                         Attributes = x.VariantId == null || x.Variant.MaterialVariantAttributes.Count <= 0 ? null : x.Variant.MaterialVariantAttributes.Select(x => new AttributeDTO()
                         {
                             Name = x.Attribute.Name,
                             Value = x.Value
                         }).ToList(),
                         LastUpdateTime = x.LastUpdateTime
                     }).ToListAsync();
                switch (quantityStatus)
                {
                    case 1:
                        //con hang
                        items = items.Where(x => x.Quantity > 0).ToList();
                        break;
                    case 2:
                        //het hang
                        items = items.Where(x => x.Quantity <= 0).ToList();
                        break;
                    case 3:
                        //tren min stock
                        items = items.Where(x => x.Quantity >= x.MinStock).ToList();
                        break;
                    case 4:
                        //duoi min stock
                        items = items.Where(x => x.Quantity < x.MinStock).ToList();
                        break;

                }
                List<WarehouseDTO> list = [];
                foreach (var item in items)
                {
                    if (item.VariantId != null)
                    {
                        var variant = _variantService.Get(x => x.Id == item.VariantId).Include(x => x.ConversionUnit)
                            .FirstOrDefault();
                        if (variant != null)
                        {
                            var subVariants = _variantService.Get(x => x.AttributeVariantId == variant.Id).
                                Include(x => x.MaterialVariantAttributes).ThenInclude(x => x.Attribute).
                                Include(x => x.Material).Include(x => x.ConversionUnit).ToList();
                            if (subVariants.Count > 0)
                            {
                                list.AddRange(subVariants.Select(x => new WarehouseDTO()
                                {
                                    Id = item.Id,
                                    MaterialId = x.MaterialId,
                                    MaterialName = x.Material.Name,
                                    MaterialCode = x.Material.MaterialCode,
                                    MaterialImage = x.Material.ImageUrl,
                                    MaterialPrice = x.Material.SalePrice,
                                    MaterialCostPrice = x.Material.CostPrice,
                                    VariantId = x.Id,
                                    VariantName = x.SKU,
                                    VariantImage = x.VariantImageUrl,
                                    Quantity = (item.Quantity - (item.InOrderQuantity ?? 0)) / x.ConversionUnit.ConversionRate,
                                    VariantPrice = x.Price,
                                    VariantCostPrice = x.CostPrice,
                                    Attributes = x.MaterialVariantAttributes.Count <= 0
                                        ? null
                                        : x.MaterialVariantAttributes.Select(x => new AttributeDTO()
                                        {
                                            Name = x.Attribute.Name,
                                            Value = x.Value
                                        }).ToList(),
                                    LastUpdateTime = item.LastUpdateTime
                                }));
                            }
                        }

                    }
                }
                items.AddRange(list);

                var result = Helpers.LinqHelpers.ToPageList(items, page == null ? 0 : (int)page - 1,
                    itemPerPage == null ? 12 : (int)itemPerPage);
                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = items.Count,
                        perPage = itemPerPage == null ? 12 : itemPerPage,
                        currentPage = page == null ? 1 : page
                    }


                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        //[HttpPost("search-and-filter")]
        //public async Task<IActionResult> Get(SAFProductsDTO safProductsDto, [FromQuery] int page, [FromQuery] int itemPerPage)
        //{
        //    try
        //    {
        //        var items = await _warehouseService.Get(x =>
        //            x.Material.Name.Contains(safProductsDto.NameKeyWord)
        //            && (safProductsDto.BrandId == null || x.Material.BrandId == safProductsDto.BrandId)
        //            && (safProductsDto.CategoryId == null || x.Material.CategoryId == safProductsDto.CategoryId)
        //        ).Include(x => x.Material).Include(x => x.Variant).Select(x => new
        //        {
        //            x.Id,
        //            x.MaterialId,
        //            MaterialName = x.Material.Name,
        //            MaterialImage = x.Material.ImageUrl,
        //            x.VariantId,
        //            VariantName = x.Variant == null ? null : x.Variant.SKU,
        //            VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
        //            Quantity = x.TotalQuantity,
        //            x.LastUpdateTime
        //        }).ToListAsync();
        //        var result = Helpers.LinqHelpers.ToPageList(items, page - 1, itemPerPage);

        //        return Ok(new
        //        {
        //            data = result,
        //            pagination = new
        //            {
        //                total = items.Count,
        //                perPage = itemPerPage,
        //                currentPage = page
        //            }


        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //    }
        //}

        //private async Task BalanceQuantity()
        //{
        //    var list = _warehouseService.Get(x => x.VariantId != null)
        //            .Include(x => x.Variant).ThenInclude(x => x.ConversionUnit)
        //            .Include(x => x.Variant).ThenInclude(x => x.AttributeVariant)
        //            .Include(x => x.Variant).ThenInclude(x => x.MaterialVariantAttributes).ToList();

        //    HashSet<Variant> check = [];
        //    ICollection<BasedQuantity> basedQuantities = [];
        //    if (list.Any())
        //    {
        //        foreach (var item in list)
        //        {
        //            if (item.Variant != null)
        //            {

        //                if (item.Variant.MaterialVariantAttributes.Count > 0)
        //                {
        //                    var cons = _conversionUnitService.Get(x => x.Id == item.MaterialId).ToList();
        //                    var based = _variantService.Get(x =>
        //                        x.ConversionUnitId == null && x.AttributeVariantId == null && x.Id == item.VariantId &&
        //                        x.MaterialVariantAttributes.Count > 0).FirstOrDefault();
        //                    if (based != null)
        //                    {
        //                        var basedQuantity = _warehouseService.Get(x => x.VariantId == item.VariantId)
        //                            .FirstOrDefault();
        //                        basedQuantities.Add(new BasedQuantity()
        //                        {
        //                            MaterialId = basedQuantity.MaterialId,
        //                            VariantId = (Guid)basedQuantity.VariantId,
        //                            Quantity = basedQuantity.TotalQuantity
        //                        });
        //                    }

        //                    foreach (var con in cons)
        //                    {
        //                        var variants = _variantService.Get(x =>
        //                            x.ConversionUnitId == con.Id && x.AttributeVariantId == item.VariantId).ToList();


        //                        if (variants.Count > 0)
        //                        {
        //                            check.AddRange(variants);
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    var cons = _conversionUnitService.Get(x => x.Id == item.MaterialId).ToList();
        //                    var based = _variantService.Get(x =>
        //                        x.ConversionUnitId == null && x.AttributeVariantId == null && x.Id == item.VariantId &&
        //                        x.MaterialVariantAttributes.Count < 0).FirstOrDefault();
        //                    if (based != null)
        //                    {
        //                        var basedQuantity = _warehouseService.Get(x => x.VariantId == item.VariantId)
        //                            .FirstOrDefault();
        //                        basedQuantities.Add(new BasedQuantity()
        //                        {
        //                            MaterialId = basedQuantity.MaterialId,
        //                            VariantId = (Guid)basedQuantity.VariantId,
        //                            Quantity = basedQuantity.TotalQuantity
        //                        });
        //                    }

        //                    foreach (var con in cons)
        //                    {
        //                        var variants = _variantService.Get(x => x.ConversionUnitId == con.Id).ToList();

        //                        if (variants.Count > 0)
        //                        {
        //                            check.AddRange(variants);
        //                        }
        //                    }
        //                }
        //            }

        //        }
        //    }
        //    var distinctList = check.DistinctBy(x => x.Id).Select(x => x.Id);
        //    foreach (var variantId in distinctList)
        //    {
        //        foreach (var item in list)
        //        {
        //            if (item.VariantId == variantId)
        //            {
        //                var baseQuantity = basedQuantities
        //                    .FirstOrDefault(x => x.MaterialId == item.MaterialId);
        //                var conversionRate = _variantService.Get(x => x.Id == item.VariantId)
        //                    .Include(x => x.ConversionUnit).Select(x => x.ConversionUnit.ConversionRate)
        //                    .FirstOrDefault();

        //                item.TotalQuantity = baseQuantity.Quantity / conversionRate;
        //                item.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();

        //            }
        //            else
        //            {
        //                var baseQuantity = basedQuantities
        //                    .FirstOrDefault(x => x.MaterialId == item.MaterialId);
        //                var conversionRate = _variantService.Get(x => x.Id == item.VariantId)
        //                    .Include(x => x.ConversionUnit).Select(x => x.ConversionUnit.ConversionRate)
        //                    .FirstOrDefault();
        //                await _warehouseService.AddAsync(new Warehouse
        //                {
        //                    Id = Guid.NewGuid(),
        //                    MaterialId = item.MaterialId,
        //                    VariantId = item.VariantId,
        //                    TotalQuantity = baseQuantity.Quantity / conversionRate,
        //                    LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
        //                });
        //            }
        //        }
        //    }
        //    await _warehouseService.SaveChangeAsync();
        //}
    }
}

