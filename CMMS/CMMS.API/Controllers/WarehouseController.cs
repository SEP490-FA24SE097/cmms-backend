using System.Xml.XPath;
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
        private readonly IMaterialService _materialService;
        private readonly IImportDetailService _importDetailService;
        private readonly ICategoryService _categoryService;
        public WarehouseController(ICategoryService categoryService, IImportDetailService importDetailService, IMaterialService materialService, IWarehouseService warehouseService, IVariantService variantService, IConversionUnitService conversionUnitService, IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _warehouseService = warehouseService;
            _conversionUnitService = conversionUnitService;
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _materialService = materialService;
            _importDetailService = importDetailService;
            _categoryService = categoryService;
        }
        [HttpGet("get-warehouse-products")]
        public async Task<IActionResult> Get([FromQuery] int? quantityStatus, [FromQuery] string? materialName, [FromQuery] int? page, [FromQuery] int? itemPerPage,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? parentCategoryId, [FromQuery] Guid? brandId)
        {
            try
            {
                // await BalanceQuantity();

                var items = await _warehouseService
                     .Get(x => (parentCategoryId == null || x.Material.Category.ParentCategoryId == parentCategoryId) && ((x.Material.Name.ToLower().Equals(materialName.ToLower()) || x.Variant.SKU.ToLower().Equals(materialName.ToLower())) || (materialName.IsNullOrEmpty() || x.Material.Name.ToLower().Contains(materialName.ToLower())) || x.Variant.SKU.ToLower().Contains(materialName.ToLower())) &&
                               (categoryId == null || x.Material.CategoryId == categoryId) && (brandId == null || x.Material.BrandId == brandId)).
                     Include(x => x.Material).ThenInclude(x => x.Brand).
                     Include(x => x.Material).ThenInclude(x => x.Unit).
                     Include(x => x.Material).ThenInclude(x => x.Category).ThenInclude(x => x.ParentCategory).
                     Include(x => x.Variant).ThenInclude(x => x.MaterialVariantAttributes).ThenInclude(x => x.Attribute).
                     Include(x => x.Variant).ThenInclude(x => x.ConversionUnit).Select(x => new WarehouseDTO()
                     {
                         Id = x.Id,
                         MaterialId = x.MaterialId,
                         MaterialCode = x.Material.MaterialCode,
                         MaterialName = x.Material.Name,
                         MaterialImage = x.Material.ImageUrl,
                         Brand = x.Material.Brand.Name,
                         Unit = x.Material.Unit.Name,
                         ParentCategory = x.Material.Category.ParentCategory.Name,
                         Category = x.Material.Category.Name,
                         Weight = x.Material.WeightValue,
                         MaterialPrice = x.Variant == null ? x.Material.SalePrice : null,
                         MaterialCostPrice = x.Variant == null ? x.Material.CostPrice : null,
                         VariantId = x.VariantId,
                         VariantName = x.Variant == null ? null : x.Variant.SKU,
                         VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
                         Quantity = x.InRequestQuantity == null ? x.TotalQuantity : x.TotalQuantity - (decimal)x.InRequestQuantity,
                         MinStock = x.Material.MinStock,
                         MaxStock = x.Material.MaxStock,
                         Discount = x.VariantId == null ? x.Material.Discount : x.Variant.Discount,
                         InOrderQuantity = x.InRequestQuantity,
                         VariantPrice = x.Variant == null ? null : x.Variant.Price,
                         VariantCostPrice = x.Variant == null ? null : x.Variant.CostPrice,
                         Attributes = x.VariantId == null || x.Variant.MaterialVariantAttributes.Count <= 0 ? null : x.Variant.MaterialVariantAttributes.Select(x => new AttributeDTO()
                         {
                             Name = x.Attribute.Name,
                             Value = x.Value
                         }).ToList(),
                         LastUpdateTime = x.LastUpdateTime,
                         IsActive = x.Material.IsActive
                     }).ToListAsync();
                foreach (var item in items)
                {
                    decimal? afterDiscountPrice = null;
                    if (!item.Discount.IsNullOrEmpty())
                    {
                        var trimDiscount = item.Discount.Trim();

                        if (trimDiscount.Contains('%'))
                        {
                            afterDiscountPrice = item.VariantId == null ?
                                item.MaterialPrice - item.MaterialPrice * decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100
                                :
                                item.VariantPrice - item.VariantPrice * decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100;
                        }
                        else
                        {
                            afterDiscountPrice = item.VariantId == null ?
                                item.MaterialPrice - decimal.Parse(trimDiscount)
                                :
                                item.VariantPrice - decimal.Parse(trimDiscount);
                        }
                        item.AfterDiscountPrice = afterDiscountPrice;
                    }

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
                                Include(x => x.Material).ThenInclude(x => x.Category).ThenInclude(x => x.ParentCategory).
                                Include(x => x.Material).ThenInclude(x => x.Brand).Include(x => x.ConversionUnit).ThenInclude(x => x.Unit).ToList();
                            if (subVariants.Count > 0)
                            {
                                //list.AddRange(subVariants.Select(x => new WarehouseDTO()
                                //{
                                //    Id = item.Id,
                                //    MaterialId = x.MaterialId,
                                //    MaterialName = x.Material.Name,
                                //    MaterialCode = x.Material.MaterialCode,
                                //    MaterialImage = x.Material.ImageUrl,
                                //    MaterialPrice = x.Material.SalePrice,
                                //    Brand = x.Material.Brand.Name,
                                //    MaterialCostPrice = x.Material.CostPrice,
                                //    VariantId = x.Id,
                                //    VariantName = x.SKU,
                                //    VariantImage = x.VariantImageUrl,
                                //    Quantity = (item.Quantity - (item.InOrderQuantity ?? 0)) / x.ConversionUnit.ConversionRate,
                                //    VariantPrice = x.Price,
                                //    VariantCostPrice = x.CostPrice,
                                //    MinStock = x.Material.MinStock / x.ConversionUnit.ConversionRate,
                                //    MaxStock = x.Material.MaxStock / x.ConversionUnit.ConversionRate,
                                //    Attributes = x.MaterialVariantAttributes.Count <= 0
                                //        ? null
                                //        : x.MaterialVariantAttributes.Select(x => new AttributeDTO()
                                //        {
                                //            Name = x.Attribute.Name,
                                //            Value = x.Value
                                //        }).ToList(),
                                //    LastUpdateTime = item.LastUpdateTime
                                //}));
                                foreach (var subVariant in subVariants)
                                {
                                    decimal? afterDiscountPrice = null;
                                    if (!subVariant.Discount.IsNullOrEmpty())
                                    {
                                        var trimDiscount = subVariant.Discount.Trim();

                                        if (trimDiscount.Contains('%'))
                                        {
                                            afterDiscountPrice = subVariant.Price - subVariant.Price * decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100;
                                        }
                                        else
                                        {
                                            afterDiscountPrice = subVariant.Price - decimal.Parse(trimDiscount);
                                        }
                                    }
                                    list.Add(new WarehouseDTO()
                                    {
                                        Id = item.Id,
                                        MaterialId = subVariant.MaterialId,
                                        MaterialName = subVariant.Material.Name,
                                        MaterialCode = subVariant.Material.MaterialCode,
                                        MaterialImage = subVariant.Material.ImageUrl,
                                        Brand = subVariant.Material.Brand.Name,
                                        Unit = subVariant.ConversionUnit.Unit.Name,
                                        ParentCategory = subVariant.Material.Category.ParentCategory.Name,
                                        Category = subVariant.Material.Category.Name,
                                        Weight = subVariant.Material.WeightValue,
                                        MaterialPrice = null,
                                        MaterialCostPrice = null,
                                        VariantId = subVariant.Id,
                                        VariantName = subVariant.SKU,
                                        VariantImage = subVariant.VariantImageUrl,
                                        //Quantity = (item.Quantity - (item.InOrderQuantity ?? 0)) / subVariant.ConversionUnit.ConversionRate,
                                        Quantity = item.Quantity / subVariant.ConversionUnit.ConversionRate,
                                        VariantPrice = subVariant.Price,
                                        VariantCostPrice = subVariant.CostPrice,
                                        MinStock = subVariant.Material.MinStock / subVariant.ConversionUnit.ConversionRate,
                                        MaxStock = subVariant.Material.MaxStock / subVariant.ConversionUnit.ConversionRate,
                                        Discount = subVariant.Discount,
                                        AfterDiscountPrice = afterDiscountPrice,
                                        Attributes = subVariant.MaterialVariantAttributes.Count <= 0
                                            ? null
                                            : subVariant.MaterialVariantAttributes.Select(x => new AttributeDTO()
                                            {
                                                Name = x.Attribute.Name,
                                                Value = x.Value
                                            }).ToList(),
                                        LastUpdateTime = item.LastUpdateTime,
                                        IsActive = item.IsActive
                                    });
                                }
                            }
                        }

                    }
                }
                items.AddRange(list);
                var suppliers = _importDetailService.GetAll().Include(x => x.Import).ThenInclude(x => x.Supplier).OrderBy(x => x.Import.TimeStamp)
                    .ToList();
                foreach (var item in items)
                {
                    foreach (var supplier in suppliers)
                    {
                        if (item.MaterialId == supplier.MaterialId && item.VariantId == supplier.VariantId)
                        {
                            item.Supplier = supplier.Import.SupplierId == null ? null : supplier.Import.Supplier.Name;
                        }
                    }
                }

                #region MyRegion

                //var extendedItems = _materialService.Get(x => !items.Select(x => x.MaterialId).Contains(x.Id)).
                //    Include(x => x.Variants).ThenInclude(x => x.MaterialVariantAttributes).ThenInclude(x => x.Attribute).
                //    Include(x => x.Variants).ThenInclude(x => x.ConversionUnit).Include(x => x.Brand)
                //    .ToList();
                //List<WarehouseDTO> extendedList = [];
                //if (extendedItems.Count > 0)
                //{
                //    foreach (var item in extendedItems)
                //    {
                //        if (item.Variants.Count > 0)
                //        {
                //            extendedList.AddRange(item.Variants.Select(x => new WarehouseDTO()
                //            {
                //                Id = Guid.NewGuid(),
                //                MaterialId = x.MaterialId,
                //                MaterialCode = x.Material.MaterialCode,
                //                MaterialName = x.Material.Name,
                //                MaterialImage = x.Material.ImageUrl,
                //                Brand = x.Material.Brand.Name,
                //                MaterialPrice = x.Material.SalePrice,
                //                MaterialCostPrice = x.Material.CostPrice,
                //                VariantId = x.Id,
                //                VariantName = x.SKU,
                //                VariantImage = x.VariantImageUrl,
                //                Quantity = 0,
                //                MinStock = x.ConversionUnitId == null ? x.Material.MinStock : x.Material.MinStock / x.ConversionUnit.ConversionRate,
                //                MaxStock = x.ConversionUnitId == null ? x.Material.MaxStock : x.Material.MaxStock / x.ConversionUnit.ConversionRate,
                //                InOrderQuantity = 0,
                //                VariantPrice = x.Price,
                //                Attributes = x.MaterialVariantAttributes.Count <= 0 ? null : x.MaterialVariantAttributes.Select(x => new AttributeDTO()
                //                {
                //                    Name = x.Attribute.Name,
                //                    Value = x.Value
                //                }).ToList(),
                //                LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                //            }));

                //        }
                //        else
                //        {
                //            extendedList.Add(new WarehouseDTO()
                //            {
                //                Id = Guid.NewGuid(),
                //                MaterialId = item.Id,
                //                MaterialCode = item.MaterialCode,
                //                MaterialName = item.Name,
                //                Brand = item.Brand.Name,
                //                MaterialImage = item.ImageUrl,
                //                MaterialPrice = item.SalePrice,
                //                MaterialCostPrice = item.CostPrice,
                //                VariantId = null,
                //                VariantName = null,
                //                VariantImage = null,
                //                Quantity = 0,
                //                MinStock = item.MinStock,
                //                MaxStock = item.MaxStock,
                //                InOrderQuantity = 0,
                //                VariantPrice = null,
                //                Attributes = null,
                //                LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                //            });
                //        }
                //    }
                //}
                //items.AddRange(extendedList);

                #endregion

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

                var result = Helpers.LinqHelpers.ToPageList(items.OrderByDescending(x => x.LastUpdateTime), page == null ? 0 : (int)page - 1,
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
                        material.Material.MinStock = dto.MinStock == null ? material.Material.MinStock : (decimal)dto.MinStock * conversionRate;
                        material.Material.MaxStock = dto.MaxStock == null ? material.Material.MaxStock : (decimal)dto.MaxStock * conversionRate;

                    }
                    else
                    {
                        material.Material.MinStock = dto.MinStock == null ? material.Material.MinStock : (decimal)dto.MinStock;
                        material.Material.MaxStock = dto.MaxStock == null ? material.Material.MaxStock : (decimal)dto.MaxStock;

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
        private async Task<decimal> GetConversionRate(Guid materialId, Guid? variantId)
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

