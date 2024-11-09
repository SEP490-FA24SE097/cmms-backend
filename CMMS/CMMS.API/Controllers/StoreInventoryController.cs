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

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/store-inventories")]
    public class StoreInventoryController : ControllerBase
    {
        private readonly IStoreInventoryService _storeInventoryService;

        public StoreInventoryController(IStoreInventoryService storeInventoryService)
        {
            _storeInventoryService = storeInventoryService;
        }




        [HttpGet("get-product-quantity")]
        public async Task<IActionResult> Get(GetProductQuantityDTO getProductQuantity)
        {
            try
            {
                var item = await _storeInventoryService.Get(x =>
                    x.StoreId == getProductQuantity.StoreId && x.MaterialId == getProductQuantity.MaterialId &&
                    x.VariantId == getProductQuantity.VariantId).FirstOrDefaultAsync();
                if (item == null)
                {
                    return Ok(new { success = true, message = "Không có hàng trong kho của cửa hàng" });
                }
                if (item.TotalQuantity == 0)
                {
                    return Ok(new { success = true, message = "Hết hàng" });
                }
                return Ok(new { success = true, quantity = item.TotalQuantity });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("get-products-by-store-id")]
        public async Task<IActionResult> Get([FromQuery] string storeId, [FromQuery] int page, [FromQuery] int itemPerPage)
        {
            try
            {
                var items = await _storeInventoryService.Get(x =>
                    x.StoreId == storeId
                    ).Include(x => x.Material).Include(x => x.Variant).Select(x => new
                    {
                        x.Id,
                        x.MaterialId,
                        MaterialName = x.Material.Name,
                        MaterialImage = x.Material.ImageUrl,
                        x.VariantId,
                        VariantName = x.Variant == null ? null : x.Variant.SKU,
                        VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
                        Quantity = x.TotalQuantity,
                        x.MinStock,
                        x.MaxStock,
                        x.LastUpdateTime
                    }).ToListAsync();
                var result = Helpers.LinqHelpers.ToPageList(items, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = items.Count,
                        perPage = itemPerPage,
                        currentPage = page
                    }


                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("create-store-material")]
        public async Task<IActionResult> Create(StoreMaterialCM storeMaterialCm)
        {
            try
            {
                await _storeInventoryService.AddAsync(new StoreInventory
                {
                    Id = new Guid(),
                    StoreId = storeMaterialCm.StoreId,
                    MaterialId = storeMaterialCm.MaterialId,
                    VariantId = storeMaterialCm.VariantId,
                    TotalQuantity = 0,
                    MinStock = storeMaterialCm.MinStock,
                    MaxStock = storeMaterialCm.MaxStock,
                    LastUpdateTime = GetVietNamTime()
                });
                await _storeInventoryService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost("search-and-filter")]
        public async Task<IActionResult> Get(SAFProductsDTO safProductsDto, [FromQuery] string storeId, [FromQuery] int page, [FromQuery] int itemPerPage)
        {
            try
            {
                var items = await _storeInventoryService.Get(x => x.StoreId == storeId &&
                    x.Material.Name.Contains(safProductsDto.NameKeyWord)
                    && (safProductsDto.BrandId == null || x.Material.BrandId == safProductsDto.BrandId)
                    && (safProductsDto.CategoryId == null || x.Material.CategoryId == safProductsDto.CategoryId)
                ).Include(x => x.Material).Include(x => x.Variant).Select(x => new
                {
                    x.Id,
                    x.MaterialId,
                    MaterialName = x.Material.Name,
                    x.VariantId,
                    VariantName = x.Variant == null ? null : x.Variant.SKU,
                    Quantity = x.TotalQuantity,
                    x.MinStock,
                    x.MaxStock,
                    x.LastUpdateTime
                }).ToListAsync();
                var result = Helpers.LinqHelpers.ToPageList(items, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = items.Count,
                        perPage = itemPerPage,
                        currentPage = page
                    }


                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
