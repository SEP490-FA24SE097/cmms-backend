using System.Collections;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMMS.API.TimeConverter;
using CMMS.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Humanizer;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;

namespace CMMS.API.Controllers
{
    [Route("api/goods-notes")]
    [ApiController]
    [AllowAnonymous]
    public class GoodsNoteController : ControllerBase
    {
        private readonly IGoodsNoteService _goodsDeliveryNoteService;
        private readonly IGoodsNoteDetailService _goodsDeliveryNoteDetailService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IWarehouseService _warehouseService;
        private readonly IVariantService _variantService;
        public GoodsNoteController(IVariantService variantService, IGoodsNoteService goodsDeliveryNoteService, IGoodsNoteDetailService goodsDeliveryNoteDetailService, IStoreInventoryService storeInventoryService, IWarehouseService warehouseService)
        {
            _goodsDeliveryNoteDetailService = goodsDeliveryNoteDetailService;
            _goodsDeliveryNoteService = goodsDeliveryNoteService;
            _storeInventoryService = storeInventoryService;
            _warehouseService = warehouseService;
            _variantService = variantService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? itemPerPage, [FromQuery] string? storeId, [FromQuery] int? type, [FromQuery] bool? isDateDescending, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var list = await _goodsDeliveryNoteService.Get(x => x.StoreId == storeId && (type == null || x.Type == type) && (from == null || x.TimeStamp >= from) && (to == null || x.TimeStamp <= to)).Include(x => x.Store)
                    .Include(x => x.GoodsNoteDetails).Select(x => new
                    {
                        id = x.Id,
                        noteCode = "Note-" + x.Id.ToString().ToUpper().Substring(0, 4),
                        storeId = x.StoreId,
                        storeName = x.Store.Name,
                        totalQuantity = x.TotalQuantity,
                        reasonDescription = x.ReasonDescription,
                        type = x.Type == 1 ? "Nhập hàng" : "Xuất hàng",
                        timeStamp = x.TimeStamp,
                        details = x.GoodsNoteDetails.Select(x => new
                        {
                            id = x.Id,
                            materialId = x.MaterialId,
                            materialName = x.Material.Name,
                            variantId = x.VariantId,
                            sku = x.Variant == null ? null : x.Variant.SKU,
                            quantity = x.Quantity
                        })
                    }).ToListAsync();
                if (isDateDescending == null)
                    list = list.OrderByDescending(x => x.timeStamp).ToList();
                else
                {
                    if ((bool)isDateDescending)
                    {
                        list = list.OrderByDescending(x => x.timeStamp).ToList();
                    }
                    if (!(bool)isDateDescending)
                    {
                        list = list.OrderBy(x => x.timeStamp).ToList();
                    }
                }
                var result = Helpers.LinqHelpers.ToPageList(list, page == null ? 0 : (int)page - 1,
                    itemPerPage == null ? 12 : (int)itemPerPage);
                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count,
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
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            try
            {
                var details = _goodsDeliveryNoteDetailService.Get(x => x.GoodsNoteId == id).Include(x => x.Material).Include(x => x.Variant).Select(x => new
                {
                    id = x.Id,
                    materialId = x.MaterialId,
                    materialName = x.Material.Name,
                    variantId = x.VariantId,
                    sku = x.Variant == null ? null : x.Variant.SKU,
                    quantity = x.Quantity,
                }).ToList();
                var result = await _goodsDeliveryNoteService.Get(x => x.Id == id).Include(x => x.Store).Include(x => x.GoodsNoteDetails).Select(x => new
                {
                    id = x.Id,
                    storeId = x.StoreId,
                    storeName = x.Store.Name,
                    totalQuantity = x.TotalQuantity,
                    reason = x.ReasonDescription,
                    type = x.Type == 1 ? "Nhập hàng" : "Xuất hàng",
                    timeStamp = x.TimeStamp,
                    details = details
                }).ToListAsync();

                return Ok(new
                {
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost("quantity-tracking")]
        public async Task<IActionResult> Create([FromBody] List<TrackingCM> goodsDeliveryNoteCm)
        {
            try
            {
                foreach (var item in goodsDeliveryNoteCm)
                {
                    if (item.QuantityInReality == item.QuantityInSystem)
                        return BadRequest("Số lượng thực tế không thể bằng số lượng trong hệ thống");
                    if (item.QuantityInReality < 0 || item.QuantityInSystem < 0)
                        return BadRequest("Số lượng không thể là số âm");
                }
                foreach (var item in goodsDeliveryNoteCm)
                {
                    if (item.StoreId == null)
                    {
                        var warehouseItem = await GetWarehouseItem(item.MaterialId, item.VariantId);
                        var conversionRate = await GetConversionRate(item.MaterialId, item.VariantId);
                        if (warehouseItem != null)
                        {
                            warehouseItem.TotalQuantity -= conversionRate > 0 ? (item.QuantityInSystem-  item.QuantityInReality) * conversionRate :  item.QuantityInSystem - item.QuantityInReality;
                            
                            await _warehouseService.SaveChangeAsync();
                        }
                        else
                        {
                            return BadRequest("Item not found!");
                        }

                    }
                    else
                    {
                        var storeItem = await GetStoreInventoryItem(item.MaterialId, item.VariantId, item.StoreId);
                        var conversionRate = await GetConversionRate(item.MaterialId, item.VariantId);

                        if (storeItem != null)
                        {
                            storeItem.TotalQuantity -= conversionRate > 0 ?  (item.QuantityInSystem- item.QuantityInReality) * conversionRate :  item.QuantityInSystem- item.QuantityInReality;
                            
                            await _storeInventoryService.SaveChangeAsync();
                        }
                        else
                        {
                            return BadRequest("Item not found!");
                        }
                    }
                    var storeName = item.StoreId == null ? "tổng" : _goodsDeliveryNoteService.Get(x => x.StoreId == item.StoreId).Include(x => x.Store)
                        .Select(x => x.Store.Name).FirstOrDefault();

                    var goodsDeliveryNote = new GoodsNote
                    {
                        Id = Guid.NewGuid(),
                        StoreId = item.StoreId,
                        ReasonDescription = item.QuantityInSystem > item.QuantityInReality ?
                            $"Xuất hàng để cân bằng số lượng sản phẩm trong kho {storeName} (số lượng thực tế: {item.QuantityInReality}, số lượng trong hệ thống: {item.QuantityInSystem})" :
                            $"Nhập hàng để cân bằng số lượng sản phẩm trong kho {storeName} (số lượng thực tế: {item.QuantityInReality}, số lượng trong hệ thống: {item.QuantityInSystem})",
                        TotalQuantity = Math.Abs(item.QuantityInSystem - item.QuantityInReality),
                        TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                        Type = item.QuantityInSystem > item.QuantityInReality ? (int)GoodsNoteType.Delivery : (int)GoodsNoteType.Receive
                    };
                    await _goodsDeliveryNoteService.AddAsync(goodsDeliveryNote);
                    await _goodsDeliveryNoteDetailService.AddAsync(new GoodsNoteDetail
                    {
                        Id = Guid.NewGuid(),
                        GoodsNoteId = goodsDeliveryNote.Id,
                        MaterialId = item.MaterialId,
                        VariantId = item.VariantId,
                        Quantity = goodsDeliveryNote.TotalQuantity,

                    });
                    await _goodsDeliveryNoteDetailService.SaveChangeAsync();

                }

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
