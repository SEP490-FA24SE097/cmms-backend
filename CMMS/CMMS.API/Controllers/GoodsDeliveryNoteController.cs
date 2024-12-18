using System.Collections;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMMS.API.TimeConverter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/goods-delivery-notes")]
    [ApiController]
    public class GoodsDeliveryNoteController : ControllerBase
    {
        private readonly IGoodsDeliveryNoteService _goodsDeliveryNoteService;
        private readonly IGoodsDeliveryNoteDetailService _goodsDeliveryNoteDetailService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IWarehouseService _warehouseService;

        public GoodsDeliveryNoteController(IGoodsDeliveryNoteService goodsDeliveryNoteService, IGoodsDeliveryNoteDetailService goodsDeliveryNoteDetailService, IStoreInventoryService storeInventoryService, IWarehouseService warehouseService)
        {
            _goodsDeliveryNoteDetailService = goodsDeliveryNoteDetailService;
            _goodsDeliveryNoteService = goodsDeliveryNoteService;
            _storeInventoryService = storeInventoryService;
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? itemPerPage)
        {
            try
            {
                var list = await _goodsDeliveryNoteService.GetAll().Include(x => x.Store).Select(x => new
                {
                    id = x.Id,
                    total = x.Total,
                    totalByText = x.TotalByText,
                    storeId = x.StoreId,
                    storeName = x.Store.Name,
                    reason = x.ReasonDescription,
                    timeStamp = x.TimeStamp
                }).ToListAsync();
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
                var details = _goodsDeliveryNoteDetailService.Get(x => x.GoodsDeliveryNoteId == id).Include(x => x.Material).Include(x => x.Variant).Select(x => new
                {
                    id = x.Id,
                    materialId = x.MaterialId,
                    materialName = x.Material.Name,
                    variantId = x.VariantId,
                    sku = x.Variant == null ? null : x.Variant.SKU,
                    total = x.Total,
                    quantity = x.Quantity,
                    unitPrice = x.UnitPrice,
                }).ToList();
                var result = await _goodsDeliveryNoteService.Get(x => x.Id == id).Include(x => x.Store).Include(x => x.GoodsDeliveryNoteDetails).Select(x => new
                {
                    id = x.Id,
                    total = x.Total,
                    totalByText = x.TotalByText,
                    storeId = x.StoreId,
                    storeName = x.Store.Name,
                    reason = x.ReasonDescription,
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
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GoodsDeliveryNoteCM goodsDeliveryNoteCm)
        {
            try
            {
                var goodsDeliveryNote = new GoodsDeliveryNote
                {
                    Id = Guid.NewGuid(),
                    Total = goodsDeliveryNoteCm.Total,
                    TotalByText = goodsDeliveryNoteCm.TotalByText,
                    StoreId = goodsDeliveryNoteCm.StoreId,
                    ReasonDescription = goodsDeliveryNoteCm.ReasonDescription,
                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime()

                };
                await _goodsDeliveryNoteService.AddAsync(goodsDeliveryNote);
                await _goodsDeliveryNoteService.SaveChangeAsync();
                await _goodsDeliveryNoteDetailService.AddRangeAsync(goodsDeliveryNoteCm.Details.Select(x => new GoodsDeliveryNoteDetail
                {
                    Id = Guid.NewGuid(),
                    GoodsDeliveryNoteId = goodsDeliveryNote.Id,
                    MaterialId = x.MaterialId,
                    VariantId = x.VariantId,
                    Total = x.Total,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,

                }));
                await _goodsDeliveryNoteDetailService.SaveChangeAsync();

                foreach (var detail in goodsDeliveryNoteCm.Details)
                {
                    if (goodsDeliveryNoteCm.StoreId.IsNullOrEmpty())
                    {
                        var item = _warehouseService.
                            Get(x => x.MaterialId == detail.MaterialId && x.VariantId == detail.VariantId).FirstOrDefault();
                        if (item != null)
                        {
                            item.TotalQuantity -= detail.Quantity;
                            item.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                            await _warehouseService.SaveChangeAsync();
                        }
                    }
                    else
                    {
                        var item = _storeInventoryService.
                            Get(x => x.MaterialId == detail.MaterialId && x.VariantId == detail.VariantId).FirstOrDefault();
                        if (item != null)
                        {
                            item.TotalQuantity -= detail.Quantity;
                            item.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                            await _storeInventoryService.SaveChangeAsync();
                        }
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
