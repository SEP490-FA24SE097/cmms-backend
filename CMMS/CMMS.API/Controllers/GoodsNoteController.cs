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
    [Route("api/goods-notes")]
    [ApiController]
    public class GoodsNoteController : ControllerBase
    {
        private readonly IGoodsNoteService _goodsDeliveryNoteService;
        private readonly IGoodsNoteDetailService _goodsDeliveryNoteDetailService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IWarehouseService _warehouseService;

        public GoodsNoteController(IGoodsNoteService goodsDeliveryNoteService, IGoodsNoteDetailService goodsDeliveryNoteDetailService, IStoreInventoryService storeInventoryService, IWarehouseService warehouseService)
        {
            _goodsDeliveryNoteDetailService = goodsDeliveryNoteDetailService;
            _goodsDeliveryNoteService = goodsDeliveryNoteService;
            _storeInventoryService = storeInventoryService;
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? itemPerPage, [FromQuery] string? storeId, [FromQuery] int? type, [FromQuery] bool? isDateDescending, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var list = await _goodsDeliveryNoteService.Get(x => x.StoreId == storeId && (type == null || x.Type == type) && (from == null || x.TimeStamp >= from) && (to == null || x.TimeStamp <= to)).Include(x => x.Store).Select(x => new
                {
                    id = x.Id,
                    noteCode = "Note-" + x.Id.ToString().ToUpper().Substring(0, 4),
                    storeId = x.StoreId,
                    storeName = x.Store.Name,
                    reasonDescription = x.ReasonDescription,
                    type = x.Type == 1 ? "Nhập hàng" : "Xuất hàng",
                    timeStamp = x.TimeStamp
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
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GoodsDeliveryNoteCM goodsDeliveryNoteCm)
        {
            try
            {
                var goodsDeliveryNote = new GoodsNote
                {
                    Id = Guid.NewGuid(),
                    StoreId = goodsDeliveryNoteCm.StoreId,
                    ReasonDescription = goodsDeliveryNoteCm.ReasonDescription,
                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime()

                };
                await _goodsDeliveryNoteService.AddAsync(goodsDeliveryNote);
                await _goodsDeliveryNoteService.SaveChangeAsync();
                await _goodsDeliveryNoteDetailService.AddRangeAsync(goodsDeliveryNoteCm.Details.Select(x => new GoodsNoteDetail
                {
                    Id = Guid.NewGuid(),
                    GoodsNoteId = goodsDeliveryNote.Id,
                    MaterialId = x.MaterialId,
                    VariantId = x.VariantId,
                    Quantity = x.Quantity,


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
