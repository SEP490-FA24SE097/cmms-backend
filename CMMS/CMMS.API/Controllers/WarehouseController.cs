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
                                    MinStock = x.Material.MinStock / x.ConversionUnit.ConversionRate,
                                    MaxStock = x.Material.MaxStock / x.ConversionUnit.ConversionRate,
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

        [HttpPost("update-warehouse-material-min-max-stock")]
        public async Task<IActionResult> Update([FromBody] WarehouseMinMaxStockUM dto)
        {
            try
            {
                var material = await GetWarehouseItem(dto.MaterialId, dto.VariantId);
                var conversionRate = await GetConversionRate(dto.MaterialId, dto.VariantId);

                if (material != null)
                {
                    if (conversionRate > 0)
                    {
                        material.Material.MinStock = dto.MinStock == null ? material.Material.MinStock : (decimal)dto.MinStock / conversionRate;
                        material.Material.MaxStock = dto.MaxStock == null ? material.Material.MaxStock : (decimal)dto.MaxStock / conversionRate;
                        material.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                    }
                    else
                    {
                        material.Material.MinStock = dto.MinStock == null ? material.Material.MinStock : (decimal)dto.MinStock;
                        material.Material.MaxStock = dto.MaxStock == null ? material.Material.MaxStock : (decimal)dto.MaxStock;
                        material.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                    }
                }
                await _warehouseService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        #region Conversion

        private async Task<Warehouse?> GetWarehouseItem(Guid materialId, Guid? variantId)
        {
            if (variantId == null)
                return await _warehouseService.Get(x => x.MaterialId == materialId && x.VariantId == variantId).Include(x => x.Material).FirstOrDefaultAsync();
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return await _warehouseService.Get(x => x.MaterialId == materialId && x.VariantId == variantId).Include(x => x.Material).FirstOrDefaultAsync();
                else
                {
                    return await _warehouseService.Get(x => x.VariantId == variant.AttributeVariantId).Include(x => x.Material).FirstOrDefaultAsync();
                }
            }
        }
        private async Task<decimal?> GetConversionRate(Guid materialId, Guid? variantId)
        {
            if (variantId == null)
                return 0;
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).Include(x => x.ConversionUnit).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return 0;

                else
                {
                    return variant.ConversionUnit.ConversionRate;
                }
            }
        }

        #endregion

    }
}

