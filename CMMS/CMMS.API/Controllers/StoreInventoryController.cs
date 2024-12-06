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
        private ICurrentUserService _currentUserService;

        public StoreInventoryController(IStoreInventoryService storeInventoryService,
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
                    var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).FirstOrDefault();
                    var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                    cartItemVM.ItemName += $" | {variantAttribute.Value}";
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
                if (item.TotalQuantity == 0)
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
                                    quantity = x.TotalQuantity - x.InOrderQuantity / variant.ConversionUnit.ConversionRate
                                }));
                            }
                        }
                    }
                }
                var item = await _storeInventoryService.Get(x =>
                    x.MaterialId == materialId &&
                    x.VariantId == variantId && x.TotalQuantity > 0).Include(x => x.Store).Select(x => new
                    {
                        storeId = x.StoreId,
                        storeName = x.Store.Name,
                        quantity = x.TotalQuantity - x.InOrderQuantity
                    }).ToListAsync();

                return Ok(new
                { data = item });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-products-by-store-id")]
        public async Task<IActionResult> Get([FromQuery] string? materialName, [FromQuery] int? page, [FromQuery] int? itemPerPage,
            [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId, [FromQuery] string storeId)
        {
            try
            {
                var items = await _storeInventoryService
                    .Get(x => x.StoreId == storeId && (materialName.IsNullOrEmpty() || x.Material.Name.ToLower().Contains(materialName.ToLower())) &&
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
                        VariantId = x.VariantId,
                        VariantName = x.Variant == null ? null : x.Variant.SKU,
                        VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
                        Quantity = x.TotalQuantity - (decimal)x.InOrderQuantity,
                        InOrderQuantity = x.InOrderQuantity,
                        VariantPrice = x.Variant == null ? null : x.Variant.Price,
                        Attributes = x.VariantId == null || x.Variant.MaterialVariantAttributes.Count <= 0 ? null : x.Variant.MaterialVariantAttributes.Select(x => new AttributeDTO()
                        {
                            Name = x.Attribute.Name,
                            Value = x.Value
                        }).ToList(),
                        LastUpdateTime = x.LastUpdateTime
                    }).ToListAsync();
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
                            list.AddRange(subVariants.Select(x => new WarehouseDTO()
                            {
                                Id = item.Id,
                                MaterialId = x.MaterialId,
                                MaterialName = x.Material.Name,
                                MaterialCode = x.Material.MaterialCode,
                                MaterialImage = x.Material.ImageUrl,
                                MaterialPrice = x.Material.SalePrice,
                                VariantId = x.Id,
                                VariantName = x.SKU,
                                VariantImage = x.VariantImageUrl,
                                Quantity = item.Quantity / x.ConversionUnit.ConversionRate - (decimal)item.InOrderQuantity / x.ConversionUnit.ConversionRate,
                                VariantPrice = x.Price,
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

        //[HttpPost("create-store-material")]
        //public async Task<IActionResult> Create(StoreMaterialCM storeMaterialCm)
        //{
        //    try
        //    {
        //        await _storeInventoryService.AddAsync(new StoreInventory
        //        {
        //            Id = new Guid(),
        //            StoreId = storeMaterialCm.StoreId,
        //            MaterialId = storeMaterialCm.MaterialId,
        //            VariantId = storeMaterialCm.VariantId,
        //            TotalQuantity = 0,
        //            MinStock = storeMaterialCm.MinStock,
        //            MaxStock = storeMaterialCm.MaxStock,
        //            LastUpdateTime = GetVietNamTime()
        //        });
        //        await _storeInventoryService.SaveChangeAsync();
        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //    }
        //}
        [HttpPost("update-store-material-min-max-stock")]
        public async Task<IActionResult> Update([FromQuery] string storeId, [FromQuery] Guid materialId, [FromQuery] Guid? variantId, [FromQuery] decimal? minStock, [FromQuery] decimal? maxStock)
        {
            try
            {
                var material = _storeInventoryService
                    .Get(x => x.StoreId == storeId && x.MaterialId == materialId && x.VariantId == variantId)
                    .FirstOrDefault();
                if (material != null)
                {
                    material.MinStock = minStock == null ? material.MinStock : (decimal)minStock;
                    material.MaxStock = maxStock == null ? material.MaxStock : (decimal)maxStock;
                    material.LastUpdateTime = GetVietNamTime();
                }
                await _storeInventoryService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        //[HttpPost("search-and-filter")]
        //public async Task<IActionResult> Get(SAFProductsDTO safProductsDto, [FromQuery] string storeId, [FromQuery] int page, [FromQuery] int itemPerPage)
        //{
        //    try
        //    {
        //        var items = await _storeInventoryService.Get(x => x.StoreId == storeId &&
        //            x.Material.Name.Contains(safProductsDto.NameKeyWord)
        //            && (safProductsDto.BrandId == null || x.Material.BrandId == safProductsDto.BrandId)
        //            && (safProductsDto.CategoryId == null || x.Material.CategoryId == safProductsDto.CategoryId)
        //        ).Include(x => x.Material).Include(x => x.Variant).Select(x => new
        //        {
        //            x.Id,
        //            x.MaterialId,
        //            MaterialName = x.Material.Name,
        //            x.VariantId,
        //            VariantName = x.Variant == null ? null : x.Variant.SKU,
        //            Quantity = x.TotalQuantity,
        //            x.MinStock,
        //            x.MaxStock,
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
    }

}

