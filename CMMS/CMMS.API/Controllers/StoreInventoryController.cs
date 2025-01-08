using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using static CMMS.API.TimeConverter.TimeConverter;
using CMMS.API.Constant;
using AutoMapper;
using CMMS.API.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using CMMS.API.Helpers;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.IdentityModel.Tokens;
using CMMS.Infrastructure.Constant;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/store-inventories")]
    public class StoreInventoryController : ControllerBase
    {
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IMapper _mapper;
        private readonly IImportDetailService _importDetailService;
        private ICurrentUserService _currentUserService;

        public StoreInventoryController(IImportDetailService importDetailService, IStoreInventoryService storeInventoryService,
            ICurrentUserService currentUserService,
            IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService,
            IMapper mapper)
        {
            _storeInventoryService = storeInventoryService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _importDetailService = importDetailService;
        }


        [HttpPost]
        public async Task<IActionResult> GetUserCartAsync(CartItemRequest cartItems)
        {
            var listCartRequest = cartItems.CartItems.ToPageList(cartItems.currentPage, cartItems.perPage);
            List<CartItemVM> listCartItemVM = new List<CartItemVM>();
            decimal totalAmount = 0;
            foreach (var cartItem in listCartRequest)
            {
                var avaibleQuantity = _storeInventoryService.GetAvailableQuantityInAllStore(cartItem);
                CartItemVM cartItemVM = _mapper.Map<CartItemVM>(cartItem);
                var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                cartItemVM.ItemName = material.Name;
                cartItemVM.SalePrice = material.SalePrice;
                cartItemVM.ImageUrl = material.ImageUrl;
                cartItemVM.ItemTotalPrice = material.SalePrice * cartItem.Quantity;
                if (avaibleQuantity < cartItem.Quantity)
                    cartItemVM.IsChangeQuantity = true;
                if (cartItem.VariantId != null)
                {
                    var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                    //var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                    //cartItemVM.ItemName += $" | {variantAttribute.Value}";
                    if (variant.MaterialVariantAttributes.Count > 0)
                    {
                        var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                        var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                        cartItemVM.ItemName += $" | {variant.SKU} {attributesString}";
                    }
                    else
                    {
                        cartItemVM.ItemName += $" | {variant.SKU}";
                    }
                    cartItemVM.SalePrice = variant.Price;
                    cartItemVM.ImageUrl = variant.VariantImageUrl;
                    cartItemVM.ItemTotalPrice = variant.Price * cartItem.Quantity;
                }
                totalAmount += cartItemVM.ItemTotalPrice;
                listCartItemVM.Add(cartItemVM);
            }

            return Ok(new
            {
                data = new
                {
                    total = totalAmount,
                    StoreItems = listCartItemVM
                },
                pagination = new
                {
                    total = cartItems.CartItems.Count(),
                    perPage = cartItems.perPage,
                    currentPage = cartItems.currentPage,
                }
            });
        }

        [HttpGet("get-product-quantity-of-specific-store")]
        public async Task<IActionResult> Get([FromQuery] Guid materialId, [FromQuery] Guid? variantId, [FromQuery] string storeId)
        {
            try
            {
                var item = await _storeInventoryService.Get(x =>
                    x.StoreId == storeId && x.MaterialId == materialId &&
                    x.VariantId == variantId).FirstOrDefaultAsync();
                if (item == null)
                {
                    return Ok(new { success = true, message = "Không có hàng trong kho của cửa hàng" });
                }
                if (item.TotalQuantity <= 0)
                {
                    return Ok(new { success = true, message = "Hết hàng" });
                }
                return Ok(new { data = new { quantity = item.TotalQuantity } });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-store-product-quantity-list")]
        public async Task<IActionResult> GetQuantity([FromQuery] Guid materialId, [FromQuery] Guid? variantId)
        {
            try
            {
                var material = _materialService.Get(x => x.Id == materialId).FirstOrDefault();
                if (!(bool)material.IsActive)
                {
                    return BadRequest();
                }
                if (variantId != null)
                {
                    var variant = _variantService.Get(x => x.Id == variantId).Include(x => x.ConversionUnit).FirstOrDefault();
                    if (variant != null)
                    {
                        if (variant.ConversionUnitId != null)
                        {
                            var rootVariantItems = _storeInventoryService
                                .Get(x => x.VariantId == variant.AttributeVariantId).Include(x => x.Store).ToList();
                            if (rootVariantItems.Count > 0)
                            {
                                return Ok(rootVariantItems.Select(x => new
                                {

                                    storeId = x.StoreId,
                                    storeName = x.Store.Name,
                                    quantity = x.InOrderQuantity == null ? x.TotalQuantity / variant.ConversionUnit.ConversionRate : (x.TotalQuantity - x.InOrderQuantity) / variant.ConversionUnit.ConversionRate
                                }));
                            }
                        }
                    }
                }
                var items = await _storeInventoryService.Get(x =>
                    x.MaterialId == materialId &&
                    x.VariantId == variantId && x.TotalQuantity > 0).Include(x => x.Store).Select(x => new
                    {
                        storeId = x.StoreId,
                        storeName = x.Store.Name,
                        quantity = x.InOrderQuantity == null ? x.TotalQuantity : x.TotalQuantity - x.InOrderQuantity
                    }).ToListAsync();

                return Ok(new { data = new { totalQuantityInAllStore = items.Sum(x => x.quantity), items } });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-products-by-store-id")]
        public async Task<IActionResult> Get([FromQuery] int? quantityStatus, [FromQuery] string? materialName, [FromQuery] int? page, [FromQuery] int? itemPerPage,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId, [FromQuery] string storeId)
        {
            try
            {

                var secondItems = await _storeInventoryService
                    .Get(x => x.Material.IsActive != false && x.StoreId == storeId && (materialName.IsNullOrEmpty() || x.Material.Name.ToLower().Contains(materialName.ToLower())) &&
                              (categoryId == null || x.Material.CategoryId == categoryId) && (brandId == null || x.Material.BrandId == brandId)).
                    Include(x => x.Material).ThenInclude(x => x.Brand).
                    Include(x => x.Variant).ThenInclude(x => x.MaterialVariantAttributes).ThenInclude(x => x.Attribute).
                    Include(x => x.Variant).ThenInclude(x => x.ConversionUnit).Select(x => new InventoryDTO()
                    {
                        Id = x.Id,
                        MaterialId = x.MaterialId,
                        MaterialCode = x.Material.MaterialCode,
                        MaterialName = x.Material.Name,
                        MaterialImage = x.Material.ImageUrl,
                        MaterialPrice = x.Material.SalePrice,
                        Brand = x.Material.Brand.Name,
                        MinStock = x.MinStock,
                        MaxStock = x.MaxStock,
                        AutoImportQuantity = x.ImportQuantity,
                        VariantId = x.VariantId,
                        VariantName = x.Variant == null ? null : x.Variant.SKU,
                        VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
                        Quantity = x.InOrderQuantity == null ? x.TotalQuantity : x.TotalQuantity - (decimal)x.InOrderQuantity,
                        InOrderQuantity = x.InOrderQuantity,
                        VariantPrice = x.Variant == null ? null : x.Variant.Price,
                        VariantCostPrice = x.Variant == null ? null : x.Variant.CostPrice,
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
                        secondItems = secondItems.Where(x => x.Quantity > 0).ToList();
                        break;
                    case 2:
                        //het hang
                        secondItems = secondItems.Where(x => x.Quantity <= 0).ToList();
                        break;
                    case 3:
                        //tren min stock
                        secondItems = secondItems.Where(x => x.Quantity >= x.MinStock).ToList();
                        break;
                    case 4:
                        //duoi min stock
                        secondItems = secondItems.Where(x => x.Quantity < x.MinStock).ToList();
                        break;

                }
                List<InventoryDTO> secondList = [];
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
                                Include(x => x.ConversionUnit).
                                Include(x => x.Material).ThenInclude(x => x.Brand).ToList();
                            secondList.AddRange(subVariants.Select(x => new InventoryDTO()
                            {
                                Id = item.Id,
                                MaterialId = x.MaterialId,
                                MaterialName = x.Material.Name,
                                MaterialCode = x.Material.MaterialCode,
                                MaterialImage = x.Material.ImageUrl,
                                Brand = x.Material.Brand.Name,
                                MaterialPrice = x.Material.SalePrice,
                                MaterialCostPrice = x.Material.CostPrice,
                                MinStock = item.MinStock / x.ConversionUnit.ConversionRate,
                                MaxStock = x.Material.MaxStock / x.ConversionUnit.ConversionRate,
                                AutoImportQuantity = item.AutoImportQuantity / x.ConversionUnit.ConversionRate,
                                VariantId = x.Id,
                                VariantName = x.SKU,
                                VariantImage = x.VariantImageUrl,
                                Quantity = item.InOrderQuantity == null ? item.Quantity / x.ConversionUnit.ConversionRate : (item.Quantity - (decimal)item.InOrderQuantity) / x.ConversionUnit.ConversionRate,
                                VariantPrice = x.Price,
                                VariantCostPrice = x.CostPrice,
                                Attributes = x.MaterialVariantAttributes.Count <= 0 ? null : x.MaterialVariantAttributes.Select(x => new AttributeDTO()
                                {
                                    Name = x.Attribute.Name,
                                    Value = x.Value
                                }).ToList(),
                                LastUpdateTime = item.LastUpdateTime
                            }));
                        }

                    }
                }
                secondItems.AddRange(secondList);
                var suppliers = _importDetailService.GetAll().Include(x => x.Import).ThenInclude(x => x.Supplier).OrderBy(x => x.Import.TimeStamp)
                    .ToList();
                foreach (var item in secondItems)
                {
                    foreach (var supplier in suppliers)
                    {
                        if (item.MaterialId == supplier.MaterialId && item.VariantId == supplier.VariantId)
                        {
                            item.Supplier = supplier.Import.SupplierId == null ? null : supplier.Import.Supplier.Name;
                        }
                    }
                }
                var secondResult = Helpers.LinqHelpers.ToPageList(secondItems.OrderByDescending(x => x.LastUpdateTime), page == null ? 0 : (int)page - 1,
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

        [HttpPost("update-store-material-min-max-stock")]
        public async Task<IActionResult> Update([FromBody] StoreMinMaxStockUM dto)
        {
            try
            {
                var material = await GetStoreInventoryItem(dto.MaterialId, dto.VariantId, dto.StoreId);
                var conversionRate = await GetConversionRate(dto.MaterialId, dto.VariantId);

                if (material != null)
                {
                    if (conversionRate > 0)
                    {
                        material.MinStock = dto.MinStock == null ? material.MinStock : (decimal)dto.MinStock / conversionRate;
                        material.MaxStock = dto.MaxStock == null ? material.MaxStock : (decimal)dto.MaxStock / conversionRate;
                        material.LastUpdateTime = GetVietNamTime();
                    }
                    else
                    {
                        material.MinStock = dto.MinStock == null ? material.MinStock : (decimal)dto.MinStock;
                        material.MaxStock = dto.MaxStock == null ? material.MaxStock : (decimal)dto.MaxStock;
                        material.LastUpdateTime = GetVietNamTime();
                    }
                }
                await _storeInventoryService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost("update-auto-import-material-quantity")]
        public async Task<IActionResult> Update([FromBody] AutoImportQuantityUM dto)
        {
            try
            {
                var material = await GetStoreInventoryItem(dto.MaterialId, dto.VariantId, dto.StoreId);
                var conversionRate = await GetConversionRate(dto.MaterialId, dto.VariantId);

                if (material != null)
                {
                    if (conversionRate > 0)
                    {
                        material.ImportQuantity = dto.ImportQuantity == null ? material.ImportQuantity : (decimal)dto.ImportQuantity / conversionRate;
                        material.LastUpdateTime = GetVietNamTime();
                    }
                    else
                    {
                        material.ImportQuantity = dto.ImportQuantity == null ? material.ImportQuantity : (decimal)dto.ImportQuantity / conversionRate;
                        material.LastUpdateTime = GetVietNamTime();
                    }
                }
                await _storeInventoryService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        #region Conversion
        private async Task<StoreInventory?> GetStoreInventoryItem(Guid materialId, Guid? variantId, string storeId)
        {
            if (variantId == null)
                return await _storeInventoryService.Get(x => x.MaterialId == materialId && x.VariantId == variantId && x.StoreId == storeId).FirstOrDefaultAsync();
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return await _storeInventoryService.Get(x => x.MaterialId == materialId && x.VariantId == variantId && x.StoreId == storeId).FirstOrDefaultAsync();
                else
                {
                    return await _storeInventoryService.Get(x => x.VariantId == variant.AttributeVariantId && x.StoreId == storeId).FirstOrDefaultAsync();
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

