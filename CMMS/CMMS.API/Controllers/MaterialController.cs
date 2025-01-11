using System.Net;
using System.Threading.Tasks.Dataflow;
using System.Xml.XPath;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using CMMS.API.TimeConverter;
using CMMS.Infrastructure.Services.Firebase;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Humanizer;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/materials")]
    public class MaterialController : Controller
    {
        private readonly IMaterialService _materialService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IImportService _importService;
        private readonly IImportDetailService _importDetailService;
        private readonly IVariantService _variantService;
        private readonly IUnitService _unitService;
        private readonly ISubImageService _subImageService;
        private readonly IConversionUnitService _conversionUnitService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IWarehouseService _warehouseService;
        public MaterialController(IWarehouseService warehouseService, IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService, IVariantService variantService, IImportDetailService importDetailService,
            IImportService importService, IConversionUnitService conversionUnitService, ISubImageService subImageService, IUnitService unitService, IStoreInventoryService storeInventoryService)
        {
            _materialService = materialService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _importService = importService;
            _variantService = variantService;
            _conversionUnitService = conversionUnitService;
            _subImageService = subImageService;
            _unitService = unitService;
            _importDetailService = importDetailService;
            _storeInventoryService = storeInventoryService;
            _warehouseService = warehouseService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetFilter([FromQuery] bool? isActive, [FromQuery] string? materialName, [FromQuery] int? page, [FromQuery] int? itemPerPage,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId, [FromQuery] decimal? lowerPrice,
            [FromQuery] decimal? upperPrice, [FromQuery] bool? isPriceDescending,
            [FromQuery] bool? isCreatedDateDescending)
        {
            try
            {
                var materials = _materialService.GetAll().Include(x => x.Brand).Include(x => x.Category)
                    .Include(x => x.Unit)
                    .Where(x => (isActive == null || x.IsActive == isActive) && (materialName.IsNullOrEmpty() || x.Name.ToLower().Contains(materialName.ToLower())) &&
                        (categoryId == null || x.CategoryId == categoryId)
                        && (brandId == null || x.BrandId == brandId)
                        && (lowerPrice == null || x.SalePrice >= lowerPrice)
                        && (upperPrice == null || x.SalePrice <= upperPrice)
                    ).OrderByDescending(x => x.Timestamp);
                if (isPriceDescending == true)
                {
                    if (isCreatedDateDescending == true)
                        materials = materials.OrderByDescending(x => x.SalePrice).ThenByDescending(x => x.Timestamp);
                    if (isCreatedDateDescending == false)
                        materials = materials.OrderByDescending(x => x.SalePrice).ThenBy(x => x.Timestamp);
                    if (isCreatedDateDescending == null)
                        materials = materials.OrderByDescending(x => x.SalePrice);
                }

                if (isPriceDescending == false)
                {
                    if (isCreatedDateDescending == true)
                        materials = materials.OrderBy(x => x.SalePrice).ThenByDescending(x => x.Timestamp);
                    if (isCreatedDateDescending == false)
                        materials = materials.OrderBy(x => x.SalePrice).ThenBy(x => x.Timestamp);
                    if (isCreatedDateDescending == null)
                        materials = materials.OrderBy(x => x.SalePrice);
                }

                if (isPriceDescending == null)
                {
                    if (isCreatedDateDescending == true)

                        materials = materials.OrderByDescending(x => x.Timestamp);

                    if (isCreatedDateDescending == false)

                        materials = materials.OrderBy(x => x.Timestamp);

                }

                var list = materials.Select(x => new MaterialDTO()
                {
                    Id = x.Id,
                    Name = x.Name,
                    BarCode = x.BarCode,
                    Brand = x.Brand.Name,
                    IsRewardEligible = x.IsRewardEligible,
                    Description = x.Description,
                    WeightValue = x.WeightValue,
                    MaterialCode = x.MaterialCode,
                    SalePrice = x.SalePrice,
                    CostPrice = x.CostPrice,
                    Unit = x.Unit.Name,
                    Category = x.Category.Name,
                    MinStock = (decimal)x.MinStock,
                    ImageUrl = x.ImageUrl,
                    Discount = x.Discount,
                    IsActive = x.IsActive

                }).ToList();
                foreach (var material in list)
                {
                    decimal? afterDiscountPrice;
                    if (!material.Discount.IsNullOrEmpty())
                    {
                        var trimDiscount = material.Discount.Trim();

                        if (trimDiscount.Contains('%'))
                        {
                            afterDiscountPrice = material.SalePrice - material.SalePrice * decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100;
                        }
                        else
                        {
                            afterDiscountPrice = material.SalePrice - decimal.Parse(trimDiscount);
                        }

                        material.AfterDiscountPrice = afterDiscountPrice;
                    }
                }
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant).ThenInclude(x => x.ConversionUnit).ThenInclude(x => x.Unit)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            ConversionUnitId = x.Select(x => x.Variant.ConversionUnitId).FirstOrDefault(),
                            ConversionUnitName = x.Select(x => x.Variant.ConversionUnit).Any() ? x.Select(x => x.Variant.ConversionUnit.Unit.Name).FirstOrDefault() : material.Unit,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            CostPrice = x.Select(x => x.Variant.CostPrice).FirstOrDefault(),
                            Discount = x.Select(x => x.Variant.Discount).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    if (variants.Count > 0)
                    {
                        foreach (var variant in variants)
                        {
                            decimal? afterDiscountPrice;
                            if (!variant.Discount.IsNullOrEmpty())
                            {
                                var trimDiscount = variant.Discount.Trim();

                                if (trimDiscount.Contains('%'))
                                {
                                    afterDiscountPrice = variant.Price - variant.Price *
                                        decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100;

                                }
                                else
                                {
                                    afterDiscountPrice = variant.Price - decimal.Parse(trimDiscount);
                                }
                                variant.AfterDiscountPrice = afterDiscountPrice;
                            }

                        }
                    }


                    if (variants.Count <= 0)
                    {
                        var unitVariants = _variantService.Get(x => x.MaterialId == material.Id)
                            .Include(x => x.ConversionUnit).ThenInclude(x => x.Unit);
                        if (unitVariants.Any())
                        {
                            foreach (var unitVariant in unitVariants)
                            {
                                decimal? afterDiscountPrice = null;
                                if (!unitVariant.Discount.IsNullOrEmpty())
                                {
                                    var trimDiscount = unitVariant.Discount.Trim();

                                    if (trimDiscount.Contains('%'))
                                    {
                                        afterDiscountPrice = unitVariant.Price - unitVariant.Price *
                                            decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100;

                                    }
                                    else
                                    {
                                        afterDiscountPrice = unitVariant.Price - decimal.Parse(trimDiscount);
                                    }
                                }
                                variants.Add(new VariantDTO()
                                {
                                    VariantId = unitVariant.Id,
                                    ConversionUnitId = unitVariant.ConversionUnitId == null ? null : unitVariant.ConversionUnitId,
                                    ConversionUnitName = unitVariant.ConversionUnitId == null ? material.Unit : unitVariant.ConversionUnit.Unit.Name,
                                    Sku = unitVariant.SKU,
                                    Image = unitVariant.VariantImageUrl,
                                    Price = unitVariant.Price,
                                    CostPrice = unitVariant.CostPrice,
                                    Discount = unitVariant.Discount,
                                    AfterDiscountPrice = afterDiscountPrice,
                                    Attributes = null
                                });
                            }
                        }

                        //variants.AddRange(unitVariants.Include(x => x.ConversionUnit).ThenInclude(x => x.Unit).Select(x => new VariantDTO()
                        //{
                        //    VariantId = x.Id,
                        //    ConversionUnitId = x.ConversionUnitId,
                        //    ConversionUnitName = x.ConversionUnit.Unit.Name,
                        //    Sku = x.SKU,
                        //    Image = x.VariantImageUrl,
                        //    Price = x.Price,
                        //    CostPrice = x.CostPrice,
                        //    Discount = x.Discount,
                        //    Attributes = null
                        //}));
                    }
                    
                    List<VariantDTO> orderedVariants = new List<VariantDTO>(variants.Count);
                    
                    foreach (var variant in variants)
                    {
                        if (variant.ConversionUnitId == null)
                        {
                            orderedVariants.Insert(0,variant);
                        }
                        else
                        {
                            orderedVariants.Add(variant);
                        }
                    }
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = orderedVariants

                    });
                }

                var result = Helpers.LinqHelpers.ToPageList(newList, page == null ? 0 : (int)page - 1,
                    itemPerPage == null ? 12 : (int)itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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
        //[HttpGet("search")]
        //[AllowAnonymous]
        //public IActionResult Search([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string? materialName)
        //{
        //    try
        //    {
        //        var list = _materialService.GetAll().
        //            Include(x => x.Brand).
        //            Include(x => x.Category).
        //            Include(x => x.Unit).
        //            Include(x => x.Supplier).Where(x => x.Name.Contains(materialName)).Select(x => new MaterialDTO()
        //            {
        //                Id = x.Id,
        //                Name = x.Name,
        //                BarCode = x.BarCode,
        //                Brand = x.Brand.Name,
        //                IsRewardEligible = x.IsRewardEligible,
        //                Description = x.Description,

        //                SalePrice = x.SalePrice,
        //                Unit = x.Unit.Name,
        //                Supplier = x.Supplier.Name,
        //                Category = x.Category.Name,
        //                MinStock = x.MinStock,
        //                ImageUrl = x.ImageUrl

        //            }).ToList();
        //        List<MaterialVariantDTO> newList = [];
        //        foreach (var material in list)
        //        {
        //            var variants = _materialVariantAttributeService.GetAll()
        //                .Include(x => x.Variant)
        //                .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
        //                .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
        //                {
        //                    VariantId = x.Key,
        //                    Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
        //                    Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
        //                    Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
        //                    Attributes = x.Select(x => new AttributeDTO()
        //                    {
        //                        Name = x.Attribute.Name,
        //                        Value = x.Value
        //                    }).ToList()
        //                }).ToList();
        //            newList.Add(new MaterialVariantDTO()
        //            {
        //                Material = material,
        //                Variants = variants

        //            });
        //        }
        //        var result = Helpers.LinqHelpers.ToPageList(newList, page - 1, itemPerPage);

        //        return Ok(new
        //        {
        //            data = result,
        //            pagination = new
        //            {
        //                total = list.Count(),
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

        //[HttpGet("get-materials-by-category")]
        //[AllowAnonymous]
        //public IActionResult GetByCategory([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string categoryId, [FromQuery] bool isDescendingPrice)
        //{
        //    try
        //    {
        //        var list = _materialService.GetAll().Include(x => x.Brand).
        //            Include(x => x.Category).
        //            Include(x => x.Unit).
        //            Include(x => x.Supplier).Where(x => x.CategoryId == Guid.Parse(categoryId)).Select(x => new MaterialDTO()
        //            {
        //                Id = x.Id,
        //                Name = x.Name,
        //                BarCode = x.BarCode,
        //                Brand = x.Brand.Name,
        //                IsRewardEligible = x.IsRewardEligible,
        //                Description = x.Description,

        //                SalePrice = x.SalePrice,
        //                Unit = x.Unit.Name,
        //                Supplier = x.Supplier.Name,
        //                Category = x.Category.Name,
        //                MinStock = x.MinStock,
        //                ImageUrl = x.ImageUrl

        //            }).ToList();
        //        List<MaterialVariantDTO> newList = [];
        //        foreach (var material in list)
        //        {
        //            var variants = _materialVariantAttributeService.GetAll()
        //                .Include(x => x.Variant)
        //                .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
        //                .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
        //                {
        //                    VariantId = x.Key,
        //                    Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
        //                    Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
        //                    Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
        //                    Attributes = x.Select(x => new AttributeDTO()
        //                    {
        //                        Name = x.Attribute.Name,
        //                        Value = x.Value
        //                    }).ToList()
        //                }).ToList();
        //            newList.Add(new MaterialVariantDTO()
        //            {
        //                Material = material,
        //                Variants = variants

        //            });
        //        }
        //        List<MaterialVariantDTO> sortList = [];
        //        sortList = isDescendingPrice ? newList.OrderBy(x => x.Material.SalePrice).Reverse().ToList()
        //            : newList.OrderBy(x => x.Material.SalePrice).ToList();
        //        var result = Helpers.LinqHelpers.ToPageList(sortList, page - 1, itemPerPage);

        //        return Ok(new
        //        {
        //            data = result,
        //            pagination = new
        //            {
        //                total = list.Count(),
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
        //[HttpGet("get-materials-by-brand")]
        //[AllowAnonymous]
        //public IActionResult GetByBrand([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string brandId, [FromQuery] bool isDescendingPrice)
        //{
        //    try
        //    {
        //        var list = _materialService.GetAll().Include(x => x.Brand).
        //            Include(x => x.Category).
        //            Include(x => x.Unit).
        //            Include(x => x.Supplier).Where(x => x.BrandId == Guid.Parse(brandId)).Select(x => new MaterialDTO()
        //            {
        //                Id = x.Id,
        //                Name = x.Name,
        //                BarCode = x.BarCode,
        //                Brand = x.Brand.Name,
        //                IsRewardEligible = x.IsRewardEligible,
        //                Description = x.Description,

        //                SalePrice = x.SalePrice,
        //                Unit = x.Unit.Name,
        //                Supplier = x.Supplier.Name,
        //                Category = x.Category.Name,
        //                MinStock = x.MinStock,
        //                ImageUrl = x.ImageUrl

        //            }).ToList();
        //        List<MaterialVariantDTO> newList = [];
        //        foreach (var material in list)
        //        {
        //            var variants = _materialVariantAttributeService.GetAll()
        //                .Include(x => x.Variant)
        //                .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
        //                .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
        //                {
        //                    VariantId = x.Key,
        //                    Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
        //                    Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
        //                    Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
        //                    Attributes = x.Select(x => new AttributeDTO()
        //                    {
        //                        Name = x.Attribute.Name,
        //                        Value = x.Value
        //                    }).ToList()
        //                }).ToList();
        //            newList.Add(new MaterialVariantDTO()
        //            {
        //                Material = material,
        //                Variants = variants

        //            });
        //        }
        //        List<MaterialVariantDTO> sortList = [];
        //        sortList = isDescendingPrice ? newList.OrderBy(x => x.Material.SalePrice).Reverse().ToList()
        //            : newList.OrderBy(x => x.Material.SalePrice).ToList();
        //        var result = Helpers.LinqHelpers.ToPageList(sortList, page - 1, itemPerPage);

        //        return Ok(new
        //        {
        //            data = result,
        //            pagination = new
        //            {
        //                total = list.Count(),
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
        //[HttpGet("get-materials-in-price-range")]
        //[AllowAnonymous]
        //public IActionResult GetByPrice([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] decimal lowerPrice, [FromQuery] decimal upperPrice)
        //{
        //    try
        //    {
        //        var list = _materialService.GetAll().Include(x => x.Brand).
        //            Include(x => x.Category).
        //            Include(x => x.Unit).
        //            Include(x => x.Supplier).Where(x => x.SalePrice >= lowerPrice && x.SalePrice <= upperPrice).Select(x => new MaterialDTO()
        //            {
        //                Id = x.Id,
        //                Name = x.Name,
        //                BarCode = x.BarCode,
        //                Brand = x.Brand.Name,
        //                IsRewardEligible = x.IsRewardEligible,
        //                Description = x.Description,

        //                SalePrice = x.SalePrice,
        //                Unit = x.Unit.Name,
        //                Supplier = x.Supplier.Name,
        //                Category = x.Category.Name,
        //                MinStock = x.MinStock,
        //                ImageUrl = x.ImageUrl

        //            }).ToList();
        //        List<MaterialVariantDTO> newList = [];
        //        foreach (var material in list)
        //        {
        //            var variants = _materialVariantAttributeService.GetAll()
        //                .Include(x => x.Variant)
        //                .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
        //                .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
        //                {
        //                    VariantId = x.Key,
        //                    Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
        //                    Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
        //                    Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
        //                    Attributes = x.Select(x => new AttributeDTO()
        //                    {
        //                        Name = x.Attribute.Name,
        //                        Value = x.Value
        //                    }).ToList()
        //                }).ToList();
        //            newList.Add(new MaterialVariantDTO()
        //            {
        //                Material = material,
        //                Variants = variants
        //            });
        //        }
        //        List<MaterialVariantDTO> sortList = [];
        //        sortList = newList.OrderBy(x => x.Material.SalePrice).ToList();
        //        var result = Helpers.LinqHelpers.ToPageList(sortList, page - 1, itemPerPage);

        //        return Ok(new
        //        {
        //            data = result,
        //            pagination = new
        //            {
        //                total = list.Count(),
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
        //[HttpGet("get-materials-by-supplier")]
        //[AllowAnonymous]
        //public IActionResult GetBySupplier([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string supplierId)
        //{
        //    try
        //    {
        //        var list = _materialService.GetAll().Include(x => x.Brand).
        //            Include(x => x.Category).
        //            Include(x => x.Unit).
        //            Include(x => x.Supplier).Where(x => x.SupplierId == Guid.Parse(supplierId)).Select(x => new MaterialDTO()
        //            {
        //                Id = x.Id,
        //                Name = x.Name,
        //                BarCode = x.BarCode,
        //                Brand = x.Brand.Name,
        //                IsRewardEligible = x.IsRewardEligible,
        //                Description = x.Description,

        //                SalePrice = x.SalePrice,
        //                Unit = x.Unit.Name,
        //                Supplier = x.Supplier.Name,
        //                Category = x.Category.Name,
        //                MinStock = x.MinStock,
        //                ImageUrl = x.ImageUrl

        //            }).ToList();
        //        List<MaterialVariantDTO> newList = [];
        //        foreach (var material in list)
        //        {
        //            var variants = _materialVariantAttributeService.GetAll()
        //                .Include(x => x.Variant)
        //                .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
        //                .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
        //                {
        //                    VariantId = x.Key,
        //                    Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
        //                    Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
        //                    Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
        //                    Attributes = x.Select(x => new AttributeDTO()
        //                    {
        //                        Name = x.Attribute.Name,
        //                        Value = x.Value
        //                    }).ToList()
        //                }).ToList();
        //            newList.Add(new MaterialVariantDTO()
        //            {
        //                Material = material,
        //                Variants = variants

        //            });
        //        }
        //        var result = Helpers.LinqHelpers.ToPageList(newList, page - 1, itemPerPage);

        //        return Ok(new
        //        {
        //            data = result,
        //            pagination = new
        //            {
        //                total = list.Count(),
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

        [HttpGet("get-import-products")]
        public async Task<IActionResult> GetImportProducts([FromQuery] string? materialName, [FromQuery] int? page, [FromQuery] int? itemPerPage,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId)
        {
            try
            {

                var secondItems = await _variantService
                    .Get(x => x.ConversionUnitId == null && (materialName.IsNullOrEmpty() || x.Material.Name.ToLower().Contains(materialName.ToLower())) &&
                              (categoryId == null || x.Material.CategoryId == categoryId) && (brandId == null || x.Material.BrandId == brandId)).
                    Include(x => x.Material).Include(x => x.MaterialVariantAttributes).ThenInclude(x => x.Attribute).
                    Include(x => x.ConversionUnit).Select(x => new WarehouseDTO()
                    {
                        Id = Guid.NewGuid(),
                        MaterialId = x.MaterialId,
                        MaterialCode = x.Material.MaterialCode,
                        MaterialName = x.Material.Name,
                        MaterialImage = x.Material.ImageUrl,
                        MaterialPrice = x.Material.SalePrice,
                        VariantId = x.Id,
                        VariantName = x.SKU,
                        VariantImage = x.VariantImageUrl,
                        Quantity = 0,
                        InOrderQuantity = 0,
                        VariantPrice = x.Price,
                        Attributes = x.MaterialVariantAttributes.Count <= 0 ? null : x.MaterialVariantAttributes.Select(x => new AttributeDTO()
                        {
                            Name = x.Attribute.Name,
                            Value = x.Value
                        }).ToList(),
                        LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                    }).ToListAsync();
                List<WarehouseDTO> materials = [];

                //var check = _materialService.Get(x => x.IsActive != false).ToList().ExceptBy(secondItems.Select(x => x.MaterialId), x => x.Id).ToList();
                //materials.AddRange(check.Select(x => new WarehouseDTO()
                //{
                //    Id = Guid.NewGuid(),
                //    MaterialId = x.Id,
                //    MaterialCode = x.MaterialCode,
                //    MaterialName = x.Name,
                //    MaterialImage = x.ImageUrl,
                //    MaterialPrice = x.SalePrice,
                //    VariantId = null,
                //    VariantName = null,
                //    VariantImage = null,
                //    Quantity = 0,
                //    InOrderQuantity = 0,
                //    VariantPrice = null,
                //    Attributes = null,
                //    LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                //}));
                //secondItems.AddRange(materials);
                List<WarehouseDTO> secondList = [];
                foreach (var item in secondItems)
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
                            secondList.AddRange(subVariants.Select(x => new WarehouseDTO()
                            {
                                Id = Guid.NewGuid(),
                                MaterialId = x.MaterialId,
                                MaterialName = x.Material.Name,
                                MaterialCode = x.Material.MaterialCode,
                                MaterialImage = x.Material.ImageUrl,
                                MaterialPrice = x.Material.SalePrice,
                                VariantId = x.Id,
                                VariantName = x.SKU,
                                VariantImage = x.VariantImageUrl,
                                Quantity = item.InOrderQuantity == null ? item.Quantity / x.ConversionUnit.ConversionRate : (item.Quantity - (decimal)item.InOrderQuantity) / x.ConversionUnit.ConversionRate,
                                VariantPrice = x.Price,
                                Attributes = x.MaterialVariantAttributes.Count <= 0 ? null : x.MaterialVariantAttributes.Select(x => new AttributeDTO()
                                {
                                    Name = x.Attribute.Name,
                                    Value = x.Value
                                }).ToList(),
                                LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                            }));
                        }

                    }
                }
                secondItems.AddRange(secondList);
                var secondResult = Helpers.LinqHelpers.ToPageList(secondItems, page == null ? 0 : (int)page - 1,
                    itemPerPage == null ? 12 : (int)itemPerPage);
                return Ok(new
                {
                    data = secondResult,
                    pagination = new
                    {
                        total = secondItems.Count,
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
        //[HttpGet("get-all-materials-for-import")]
        //[AllowAnonymous]
        //public IActionResult GetAllNames([FromQuery] string? materialName,
        //    [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId)
        //{
        //    try
        //    {
        //        var result = _materialService.Get(x => (materialName.IsNullOrEmpty() || x.Name.ToLower().Contains(materialName.ToLower())) &&
        //                                          (categoryId == null || x.CategoryId == categoryId) && (brandId == null || x.BrandId == brandId)).Include(x => x.Variants).ToList();
        //        List<ImportProductDTO> list = [];
        //        foreach (var item in result)
        //        {
        //            if (item.Variants.Count > 0)
        //            {
        //                list.AddRange(item.Variants.Select(x => new ImportProductDTO
        //                {
        //                    MaterialId = item.Id,
        //                    MaterialName = item.Name == null ? null : item.Name,
        //                    SalePrice = item.SalePrice,
        //                    CostPrice = item.CostPrice,
        //                    Image = item.ImageUrl == null ? null : item.ImageUrl,
        //                    VariantId = x.Id,
        //                    Sku = x.SKU,
        //                    VariantImage = x.VariantImageUrl,
        //                    VariantSalePrice = x.Price,
        //                    VariantCostPrice = x.CostPrice
        //                }).ToList());
        //            }
        //            else
        //            {
        //                list.Add(new ImportProductDTO()
        //                {
        //                    MaterialId = item.Id,
        //                    MaterialName = item.Name == null ? null : item.Name,
        //                    SalePrice = item.SalePrice,
        //                    CostPrice = item.CostPrice,
        //                    Image = item.ImageUrl == null ? null : item.ImageUrl,
        //                    VariantId = null,
        //                    Sku = null,
        //                    VariantImage = null,
        //                    VariantSalePrice = null,
        //                    VariantCostPrice = null
        //                });
        //            }
        //        }

        //        return Ok(new { data = list });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //    }
        //}

        //[HttpGet("{id}")]
        //public IActionResult GetMaterialByIdAsync([FromRoute] string id)
        //{
        //    try
        //    {
        //        var result = _materialService.Get(x => x.Id == Guid.Parse(id)).Include(x => x.Brand)
        //            .Include(x => x.Category).Include(x => x.Unit).Include(x => x.SubImages).Select(x =>
        //                new MaterialDTO()
        //                {
        //                    Id = x.Id,
        //                    Name = x.Name,
        //                    BarCode = x.BarCode,
        //                    Brand = x.Brand.Name,
        //                    IsRewardEligible = x.IsRewardEligible,
        //                    Description = x.Description,
        //                    MaterialCode = x.MaterialCode,
        //                    SalePrice = x.SalePrice,
        //                    Unit = x.Unit.Name,
        //                    Category = x.Category.Name,
        //                    MinStock = (decimal)x.MinStock,
        //                    ImageUrl = x.ImageUrl,
        //                    SubImages = x.SubImages.Select(x => new SubImageDTO()
        //                    {
        //                        Id = x.Id,
        //                        SubImageUrl = x.SubImageUrl
        //                    }).ToList()
        //                }).FirstOrDefault();
        //        var variants = _materialVariantAttributeService.GetAll()
        //            .Include(x => x.Variant).ThenInclude(x => x.ConversionUnit)
        //            .Include(x => x.Attribute)
        //            .Where(x => x.Variant.MaterialId == Guid.Parse(id)).GroupBy(x => x.VariantId).ToList();

        //        if (variants.Count <= 0)
        //        {
        //            var unitVariants = _variantService.Get(x => x.MaterialId == Guid.Parse(id))
        //                .Include(x => x.ConversionUnit).ToList();
        //            return Ok(new
        //            {
        //                data = new
        //                {
        //                    material = result,
        //                    variants = unitVariants.AsQueryable().Include(x => x.ConversionUnit).ThenInclude(x => x.Unit).Select(x => new
        //                    {
        //                        VariantId = x.Id,
        //                        ConversionUnitId = x.ConversionUnitId,
        //                        ConversionUnitName = x.ConversionUnit.Unit.Name,
        //                        Sku = x.SKU,
        //                        Image = x.VariantImageUrl,
        //                        Price = x.Price,
        //                        CostPrice = x.CostPrice
        //                    })
        //                }
        //            });

        //        }

        //        return Ok(new
        //        {
        //            data = new
        //            {
        //                material = result,
        //                variants = variants.Select(x => new
        //                {
        //                    variantId = x.Key,
        //                    sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
        //                    ConversionUnitId = x.Select(x => x.Variant.ConversionUnitId).FirstOrDefault(),
        //                    ConversionUnitName = x.AsQueryable().Include(x => x.Variant).ThenInclude(x => x.ConversionUnit).ThenInclude(x => x.Unit).Select(x => x.Variant.ConversionUnit).Any() ? null : x.Select(x => x.Variant.ConversionUnit.Unit.Name).FirstOrDefault(),
        //                    image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
        //                    price = x.Select(x => x.Variant.Price).FirstOrDefault(),
        //                    CostPrice = x.Select(x => x.Variant.CostPrice).FirstOrDefault(),
        //                    attributes = x.Select(x => new
        //                    {
        //                        x.Attribute.Name,
        //                        x.Value
        //                    })
        //                })
        //            }

        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //    }
        //}

        [HttpGet("{id}")]
        public IActionResult GetMaterialByIdAsync([FromRoute] string id)
        {
            try
            {
                var result = _materialService.Get(x => x.Id == Guid.Parse(id)).Include(x => x.Brand)
                    .Include(x => x.Category).Include(x => x.Unit).Include(x => x.SubImages).Select(x =>
                        new MaterialDTO()
                        {
                            Id = x.Id,
                            Name = x.Name,
                            BarCode = x.BarCode,
                            Brand = x.Brand.Name,
                            IsRewardEligible = x.IsRewardEligible,
                            WeightValue = x.WeightValue,
                            Description = x.Description,
                            MaterialCode = x.MaterialCode,
                            SalePrice = x.SalePrice,
                            CostPrice = x.CostPrice,
                            Discount = x.Discount,
                            Unit = x.Unit.Name,
                            Category = x.Category.Name,
                            MinStock = (decimal)x.MinStock,
                            ImageUrl = x.ImageUrl,
                            IsActive = x.IsActive,
                            SubImages = x.SubImages.Select(x => new SubImageDTO()
                            {
                                Id = x.Id,
                                SubImageUrl = x.SubImageUrl
                            }).ToList()
                        }).FirstOrDefault();
                decimal? afterDiscountPrice = null;
                if (!result.Discount.IsNullOrEmpty())
                {
                    var trimDiscount = result.Discount.Trim();

                    if (trimDiscount.Contains('%'))
                    {
                        afterDiscountPrice = result.SalePrice - result.SalePrice *
                            decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100;
                    }
                    else
                    {
                        afterDiscountPrice = result.SalePrice - decimal.Parse(trimDiscount);
                    }
                    result.AfterDiscountPrice = afterDiscountPrice;
                }
                var variants = _materialVariantAttributeService.GetAll()
                    .Include(x => x.Variant).ThenInclude(x => x.ConversionUnit).ThenInclude(x => x.Unit)
                    .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == result.Id).ToList()
                    .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                    {
                        VariantId = x.Key,
                        ConversionUnitId = x.Select(x => x.Variant.ConversionUnitId).FirstOrDefault(),
                        ConversionUnitName = x.Select(x => x.Variant.ConversionUnit).Any() ? x.Select(x => x.Variant.ConversionUnit.Unit.Name).FirstOrDefault() : result.Unit,
                        Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                        Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                        Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                        CostPrice = x.Select(x => x.Variant.CostPrice).FirstOrDefault(),
                        Attributes = x.Select(x => new AttributeDTO()
                        {
                            Name = x.Attribute.Name,
                            Value = x.Value
                        }).ToList()
                    }).ToList();

                if (variants.Count <= 0)
                {
                    var unitVariants = _variantService.Get(x => x.MaterialId == Guid.Parse(id))
                        .Include(x => x.ConversionUnit).ThenInclude(x => x.Unit);
                    //variants.AddRange(unitVariants.Include(x => x.ConversionUnit).ThenInclude(x => x.Unit).Select(x => new VariantDTO()
                    //{
                    //    VariantId = x.Id,
                    //    ConversionUnitId = x.ConversionUnitId,
                    //    ConversionUnitName = x.ConversionUnit.Unit.Name,
                    //    Sku = x.SKU,
                    //    Image = x.VariantImageUrl,
                    //    Price = x.Price,
                    //    CostPrice = x.CostPrice,
                    //    Attributes = null
                    //}));

                    foreach (var unitVariant in unitVariants)
                    {
                        decimal? variantAfterDiscountPrice = null;
                        if (!unitVariant.Discount.IsNullOrEmpty())
                        {
                            var trimDiscount = unitVariant.Discount.Trim();

                            if (trimDiscount.Contains('%'))
                            {
                                variantAfterDiscountPrice = unitVariant.Price - unitVariant.Price *
                                    decimal.Parse(trimDiscount.Remove(trimDiscount.Length - 1, 1)) / 100;

                            }
                            else
                            {
                                variantAfterDiscountPrice = unitVariant.Price - decimal.Parse(trimDiscount);
                            }
                        }
                        variants.Add(new VariantDTO()
                        {
                            VariantId = unitVariant.Id,
                            ConversionUnitId = unitVariant.ConversionUnitId == null ? null : unitVariant.ConversionUnitId,
                            ConversionUnitName = unitVariant.ConversionUnitId == null ? result.Unit : unitVariant.ConversionUnit.Unit.Name,
                            Sku = unitVariant.SKU,
                            Image = unitVariant.VariantImageUrl,
                            Price = unitVariant.Price,
                            CostPrice = unitVariant.CostPrice,
                            Discount = unitVariant.Discount,
                            AfterDiscountPrice = variantAfterDiscountPrice,
                            Attributes = null
                        });
                    }
                }
                List<VariantDTO> orderedVariants = new List<VariantDTO>(variants.Count);

                foreach (var variant in variants)
                {
                    if (variant.ConversionUnitId == null)
                    {
                        orderedVariants.Insert(0, variant);
                    }
                    else
                    {
                        orderedVariants.Add(variant);
                    }
                }
                return Ok(new
                {
                    data = new
                    {
                        material = result,
                        variants = orderedVariants
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MaterialCM.PostMaterialRequest materialCm)
        {
            try
            {
                if (_materialService.Get(x => x.Name == materialCm.Name).FirstOrDefault() != null)
                {
                    return BadRequest("Name can not be duplicated!");
                }

                var mainImage = await UploadImages.UploadToFirebase([materialCm.MainImage]);

                var newGuid = Guid.NewGuid();
                var material = new Material
                {
                    Id = newGuid,
                    MaterialCode = "MAT-" + newGuid.ToString().ToUpper().Substring(0, 4),
                    Name = materialCm.Name,
                    BarCode = materialCm.Barcode,
                    Description = materialCm.Description,
                    IsActive = true,
                    WeightValue = materialCm.WeightValue,
                    ImageUrl = mainImage.First(),
                    SalePrice = materialCm.SalePrice,
                    CostPrice = materialCm.CostPrice,
                    MinStock = materialCm.MinStock,
                    MaxStock = materialCm.MaxStock,
                    BrandId = materialCm.BrandId,
                    UnitId = materialCm.BasicUnitId,
                    CategoryId = materialCm.CategoryId,
                    Timestamp = TimeConverter.TimeConverter.GetVietNamTime(),
                    IsRewardEligible = materialCm.IsPoint != null ? true : false
                };
                await _materialService.AddAsync(material);
                await _materialService.SaveChangeAsync();
                if (materialCm.SubImages.Any())
                {
                    var images = await UploadImages.UploadToFirebase(materialCm.SubImages);
                    await _subImageService.AddRange(images.Select(x => new SubImage()
                    {
                        Id = Guid.NewGuid(),
                        SubImageUrl = x,
                        MaterialId = material.Id
                    }));
                    await _subImageService.SaveChangeAsync();
                }
                if (materialCm.MaterialUnitDtoList != null && materialCm.MaterialUnitDtoList.Any())
                {
                    var list = materialCm.MaterialUnitDtoList.Select(x => new ConversionDTO()
                    {
                        Id = Guid.NewGuid(),
                        UnitId = x.UnitId,
                        ConversionRate = x.ConversionRate,
                        Price = x.Price,
                        CostPrice = x.CostPrice,
                        MaterialId = material.Id
                    }).ToList();
                    await _conversionUnitService.AddRange(list);
                    await _conversionUnitService.SaveChangeAsync();
                    var newMaterial = _materialService.Get(x => x.Id == material.Id).Include(x => x.Unit).FirstOrDefault();
                    var newVariant = new Variant()
                    {
                        Id = Guid.NewGuid(),
                        VariantImageUrl = material.ImageUrl,
                        Price = material.SalePrice,
                        CostPrice = material.CostPrice,
                        ConversionUnitId = null,
                        SKU = material.Name + " (" + newMaterial.Unit.Name + ")",
                        MaterialId = material.Id
                    };
                    await _variantService.AddAsync(newVariant);
                    foreach (var item in list)
                    {
                        var unitName = _conversionUnitService.Get(x => x.Id == item.Id).Include(x => x.Unit)
                            .FirstOrDefault();
                        await _variantService.AddAsync(new Variant()
                        {
                            Id = Guid.NewGuid(),
                            VariantImageUrl = material.ImageUrl,
                            Price = item.Price == 0 ? material.SalePrice * item.ConversionRate : item.Price,
                            CostPrice = item.CostPrice == 0 ? material.CostPrice * item.ConversionRate : item.CostPrice,
                            ConversionUnitId = item.Id,
                            AttributeVariantId = newVariant.Id,
                            SKU = material.Name + " (" + unitName.Unit.Name + ")",
                            MaterialId = material.Id
                        });
                    }
                    await _variantService.SaveChangeAsync();

                }
                var variants = _variantService.Get(x => x.MaterialId == material.Id && x.ConversionUnitId == null)
                    .ToList();
                if (variants.Count > 0)
                {
                    await _warehouseService.AddRange(variants.Select(x => new Warehouse()
                    {
                        Id = Guid.NewGuid(),
                        MaterialId = x.MaterialId,
                        VariantId = x.Id,
                        TotalQuantity = 0,
                        InRequestQuantity = 0,
                        LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                    }));
                }
                else
                {
                    await _warehouseService.AddAsync(new Warehouse()
                    {
                        Id = Guid.NewGuid(),
                        MaterialId = material.Id,
                        VariantId = null,
                        TotalQuantity = 0,
                        InRequestQuantity = 0,
                        LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                    });
                }

                await _warehouseService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] MaterialUM materialUM)
        {
            try
            {
                var material = await _materialService.FindAsync(materialUM.Id);
                material.Name = materialUM.Name.IsNullOrEmpty() ? material.Name : materialUM.Name;
                material.BarCode = materialUM.BarCode.IsNullOrEmpty() ? material.BarCode : materialUM.BarCode;
                material.Description = materialUM.Description.IsNullOrEmpty()
                    ? material.Description
                    : materialUM.Description;
                material.SalePrice = materialUM.SalePrice == 0 ? material.SalePrice : materialUM.SalePrice;
                material.CostPrice = materialUM.CostPrice == 0 ? material.CostPrice : materialUM.CostPrice;
                var variant = _variantService.Get(x => x.MaterialId == material.Id && x.ConversionUnitId == null).FirstOrDefault();
                if (variant != null)
                {
                    variant.Price = materialUM.SalePrice == 0 ? variant.Price : materialUM.SalePrice;
                    variant.CostPrice = materialUM.CostPrice == 0 ? variant.CostPrice : materialUM.CostPrice;
                }
                material.BrandId = materialUM.BrandId.IsNullOrEmpty()
                    ? material.BrandId
                    : Guid.Parse(materialUM.BrandId);
                material.CategoryId = materialUM.CategoryId.IsNullOrEmpty()
                    ? material.CategoryId
                    : Guid.Parse(materialUM.CategoryId);

                material.WeightValue = materialUM.WeightValue == null ? material.WeightValue : materialUM.WeightValue;

                if (!materialUM.MainImage.IsNullOrEmpty())
                {
                    var mainImage = await UploadImages.UploadToFirebase([materialUM.MainImage]);
                    material.ImageUrl = mainImage.First();
                }

                await _materialService.SaveChangeAsync();
                if (!materialUM.SubImages.IsNullOrEmpty())
                {
                    var images = await UploadImages.UploadToFirebase(materialUM.SubImages);
                    var existedImages = _subImageService.Get(x => x.MaterialId == materialUM.Id).ToList();
                    foreach (var image in existedImages)
                    {
                        await _subImageService.Remove(image.Id);
                    }
                    await _subImageService.AddRange(images.Select(x => new SubImage()
                    {
                        Id = Guid.NewGuid(),
                        MaterialId = materialUM.Id,
                        SubImageUrl = x
                    }));
                    await _subImageService.SaveChangeAsync();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("activate-or-deactivate-material")]
        public async Task<IActionResult> Delete([FromQuery] string materialId)
        {
            try
            {

                var material = _materialService.Get(x => x.Id == Guid.Parse(materialId)).FirstOrDefault();
                if ((bool)material.IsActive)
                {
                    material.IsActive = false;
                }

                material.IsActive = true;
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("update-sell-price")]
        public async Task<IActionResult> UpdatePrice([FromBody] UpdatePriceCM updatePriceCm)
        {
            try
            {

                if (updatePriceCm.VariantId == null)
                {
                    var material = await _materialService.FindAsync(updatePriceCm.MaterialId);
                    material.SalePrice = updatePriceCm.SellPrice;
                    var variant = _variantService.Get(x => x.MaterialId == material.Id && x.ConversionUnitId == null).FirstOrDefault();
                    if (variant != null)
                    {
                        variant.Price = updatePriceCm.SellPrice <= 0 ? variant.Price : updatePriceCm.SellPrice;

                    }
                    await _materialService.SaveChangeAsync();
                }
                else
                {
                    var variant = await _variantService.FindAsync((Guid)updatePriceCm.VariantId);
                    variant.Price = updatePriceCm.SellPrice;
                    if (variant.ConversionUnitId == null)
                    {
                        var material = _materialService.Get(x =>
                            x.Id == updatePriceCm.MaterialId).FirstOrDefault();
                        material.SalePrice = updatePriceCm.SellPrice;
                    }
                    await _variantService.SaveChangeAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost("update-material-discount")]
        public async Task<IActionResult> Update([FromBody] MaterialDiscountUM dto)
        {
            try
            {

                if (dto.VariantId != null)
                {
                    var variant = _variantService.Get(x => x.MaterialId
                        == dto.MaterialId && x.Id == dto.VariantId).FirstOrDefault();
                    if (variant != null)
                    {
                        variant.Discount = dto.Discount.Trim();
                    }
                    if (variant.ConversionUnitId == null)
                    {
                        var material = _materialService.Get(x =>
                            x.Id == dto.MaterialId).FirstOrDefault();
                        material.Discount = dto.Discount.Trim();
                    }

                }
                else
                {
                    var material = _materialService.Get(x =>
                        x.Id == dto.MaterialId).FirstOrDefault();
                    if (material != null)
                    {
                        material.Discount = dto.Discount.Trim();
                        var variant = _variantService.Get(x => x.MaterialId == material.Id && x.ConversionUnitId == null).FirstOrDefault();
                        if (variant != null)
                        {
                            variant.Discount = dto.Discount.IsNullOrEmpty() ? variant.Discount : dto.Discount.Trim();
                        }
                    }
                }

                await _materialService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        #region MyRegion

        //[AllowAnonymous]
        //[HttpGet("get-material-prices-list")]
        //public async Task<IActionResult> GetMaterialsWithPrices([FromQuery] int page, [FromQuery] int itemPerPage)
        //{
        //    try
        //    {
        //        var pricedMaterialList = new List<PricedMaterialDto>();
        //        var materialIdList = _materialService.Get(x => !x.Variants.Any()).Select(x => x.Id).ToList();
        //        foreach (var id in materialIdList)
        //        {
        //            var materials = _importDetailService.Get(x => x.MaterialId == id).Include(x => x.Material).ToList();
        //            if (materials.Any())
        //            {
        //                var latestImportPrice =
        //                    materials.AsQueryable().Include(x => x.Import).OrderByDescending(x => x.Import.TimeStamp).Select(x => x.PriceAfterDiscount / x.Quantity)
        //                        .FirstOrDefault();

        //                var averagePrice = materials.Average(x => x.PriceAfterDiscount / x.Quantity);
        //                PricedMaterialDto pricedMaterial = new()
        //                {
        //                    MaterialId = materials.FirstOrDefault().MaterialId,
        //                    MaterialName = materials.FirstOrDefault().Material.Name,
        //                    MaterialImage = materials.FirstOrDefault().Material.ImageUrl,
        //                    VariantId = null,
        //                    VariantName = null,
        //                    VariantImage = null,
        //                    LastImportPrice = latestImportPrice,
        //                    AverageImportPrice = averagePrice,
        //                    CostPrice = materials.FirstOrDefault().Material.CostPrice,
        //                    SellPrice = materials.FirstOrDefault().Material.SalePrice,
        //                };
        //                pricedMaterialList.Add(pricedMaterial);
        //            }

        //        }
        //        var variantIdList =
        //            _materialService.GetAll()
        //                .Include(x => x.Variants).Where(x => x.Variants.Any()).Select(x => x.Variants.Select(x => x.Id)).ToList();
        //        foreach (var mId in variantIdList)
        //        {
        //            foreach (var id in mId)
        //            {
        //                var materials = _importDetailService.Get(x => x.VariantId == id)
        //                    .Include(x => x.Material).Include(x => x.Variant).ToList();
        //                if (materials.Any())
        //                {
        //                    var latestImportPrice =
        //                        materials.AsQueryable().Include(x => x.Import).OrderByDescending(x => x.Import.TimeStamp).Select(x => x.PriceAfterDiscount / x.Quantity)
        //                            .FirstOrDefault();
        //                    var averagePrice = materials.Average(x => x.PriceAfterDiscount / x.Quantity);
        //                    PricedMaterialDto pricedMaterial = new()
        //                    {
        //                        MaterialId = materials.FirstOrDefault().MaterialId,
        //                        MaterialName = materials.FirstOrDefault().Material.Name,
        //                        MaterialImage = materials.FirstOrDefault().Material.ImageUrl,
        //                        VariantId = materials.FirstOrDefault().VariantId,
        //                        VariantName = materials.FirstOrDefault().Variant.SKU,
        //                        VariantImage = materials.FirstOrDefault().Variant.VariantImageUrl,
        //                        LastImportPrice = latestImportPrice,
        //                        AverageImportPrice = averagePrice,
        //                        CostPrice = materials.FirstOrDefault().Variant.CostPrice,
        //                        SellPrice = materials.FirstOrDefault().Variant.Price,
        //                    };
        //                    pricedMaterialList.Add(pricedMaterial);
        //                }
        //            }
        //        }
        //        var result = Helpers.LinqHelpers.ToPageList(pricedMaterialList, page - 1, itemPerPage);
        //        return Ok(new
        //        {
        //            data = result,
        //            pagination = new
        //            {
        //                total = pricedMaterialList.Count,
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
        //[HttpPost("upload")]
        //public async Task<IActionResult> UploadFile(List<IFormFile> file)
        //{
        //    var bucketName = "ccms-d6bf2.firebasestorage.app";
        //    GoogleCredential credential =
        //        GoogleCredential.FromFile("firebase.json");
        //    var storage = StorageClient.Create(credential);
        //    List<string> images = [];
        //    foreach (var item in file)
        //    {
        //        var objectName = $"{Path.GetRandomFileName()}_{item.FileName}";

        //        using (var stream = item.OpenReadStream())
        //        {
        //            await storage.UploadObjectAsync(bucketName, objectName, null, stream);
        //        }
        //        var publicUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{objectName}?alt=media";
        //        images.Add(publicUrl);
        //    }

        //    return Ok(images);
        //}

        #endregion


    }
}
