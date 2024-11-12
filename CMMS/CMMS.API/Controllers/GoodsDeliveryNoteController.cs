using System.Collections;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMMS.API.TimeConverter;
using Microsoft.IdentityModel.Tokens;

namespace CMMS.API.Controllers
{
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GoodsDeliveryNoteCM goodsDeliveryNoteCm)
        {
            try
            {
                var goodsDeliveryNote = new GoodsDeliveryNote
                {
                    Id = new Guid(),
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
                    Id = new Guid(),
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
