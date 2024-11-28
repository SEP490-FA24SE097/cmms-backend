using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/store-material-import-requests")]
    [ApiController]
    public class StoreMaterialImportRequestController : ControllerBase
    {
        private readonly IStoreMaterialImportRequestService _requestService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IWarehouseService _warehouseService;
        private readonly IVariantService _variantService;
        private readonly IGoodsDeliveryNoteService _goodsDeliveryNoteService;
        private readonly IGoodsDeliveryNoteDetailService _goodsDeliveryNoteDetailService;
        private readonly IStoreService _storeService;

        public StoreMaterialImportRequestController(IStoreService storeService, IGoodsDeliveryNoteService goodsDeliveryNoteService, IGoodsDeliveryNoteDetailService goodsDeliveryNoteDetailService, IWarehouseService warehouseService, IStoreMaterialImportRequestService requestService, IStoreInventoryService storeInventoryService, IVariantService variantService)
        {
            _storeInventoryService = storeInventoryService;
            _requestService = requestService;
            _variantService = variantService;
            _warehouseService = warehouseService;
            _goodsDeliveryNoteService = goodsDeliveryNoteService;
            _goodsDeliveryNoteDetailService = goodsDeliveryNoteDetailService;
            _storeService = storeService;
        }

        [HttpPost("create-store-material-import-request")]
        public async Task<IActionResult> Create([FromBody] ImportRequestCM importRequest)
        {
            try
            {
                var request = new StoreMaterialImportRequest
                {
                    Id = new Guid(),
                    StoreId = importRequest.StoreId,
                    MaterialId = importRequest.MaterialId,
                    VariantId = importRequest.VariantId,
                    Quantity = importRequest.Quantity,
                    Status = "Processing",
                    LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                };
                var check = await _requestService.CheckExist(x =>
                    x.MaterialId == request.MaterialId && x.VariantId == request.VariantId && x.Status == "Processing" && x.StoreId == request.StoreId);
                if (check)
                {
                    return BadRequest("Yêu cầu nhập hàng này đang được xử lý không thể tạo thêm yêu cầu");
                }
                await _requestService.AddAsync(request);
                await _requestService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost("cancel-processing-request")]
        public async Task<IActionResult> Create([FromQuery] Guid requestId)
        {
            try
            {
                var request = await _requestService.FindAsync(requestId);
                if (request.Status != "Processing")
                {
                    return BadRequest("The request status must be 'Processing'");
                }
                request.Status = "Canceled";
                await _requestService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }


        [HttpPost("approve-or-cancel-store-material-import-request")]
        public async Task<IActionResult> Create([FromQuery] string? fromStoreId, [FromQuery] Guid requestId, [FromQuery] bool isApproved)
        {
            try
            {
                var request = await _requestService.FindAsync(requestId);
                var item = await GetWarehouseItem(request.MaterialId, request.VariantId);
                var conversionRate = await GetConversionRate(request.MaterialId, request.VariantId);
                var importQuantity = conversionRate > 0 ? request.Quantity * conversionRate : request.Quantity;
                if (isApproved)
                {
                    if (request.Status != "Processing")
                    {
                        return BadRequest("The request status must be 'Processing'");
                    }
                    if (!fromStoreId.IsNullOrEmpty())
                    {
                        var storeInventoryItem = await GetStoreInventoryItem(request.MaterialId, request.VariantId, fromStoreId);
                        if (storeInventoryItem == null || storeInventoryItem.TotalQuantity - storeInventoryItem.InOrderQuantity < importQuantity)
                            return BadRequest("Không đủ sản phẩm trong kho cửa hàng để chuyển");
                        storeInventoryItem.InOrderQuantity = importQuantity;
                        request.FromStoreId = fromStoreId;
                        request.Status = "Approved";
                        request.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                        await _requestService.SaveChangeAsync();
                        return Ok();
                    }
                    if (item == null || item.TotalQuantity - item.InRequestQuantity < importQuantity)
                        return BadRequest("Không đủ sản phẩm trong kho");
                    item.InRequestQuantity = importQuantity;
                    request.Status = "Approved";
                }
                else
                {
                    if (request.Status != "Processing")
                    {
                        return BadRequest("The request status must be 'Processing'");
                    }
                    request.Status = "Canceled";
                }
                request.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                await _requestService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
        [HttpPost("confirm-or-cancel-store-material-import-request")]
        public async Task<IActionResult> Confirm([FromQuery] Guid requestId, [FromQuery] bool isConfirmed)
        {
            try
            {
                var request = await _requestService.FindAsync(requestId);

                if (isConfirmed)
                {
                    if (request.Status != "Approved")
                    {
                        return BadRequest("The request status must be 'Approved'");
                    }
                    request.Status = "Confirmed";
                    if (request.VariantId == null)
                    {
                        var storeInventory = _storeInventoryService
                            .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                        if (storeInventory != null)
                        {
                            storeInventory.TotalQuantity += request.Quantity;
                            storeInventory.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                        }
                        else
                        {
                            await _storeInventoryService.AddAsync(new StoreInventory()
                            {
                                Id = Guid.NewGuid(),
                                MaterialId = request.MaterialId,
                                VariantId = request.Id,
                                TotalQuantity = request.Quantity,
                                LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                            });
                        }

                        if (request.FromStoreId != null)
                        {
                            var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                .FirstOrDefaultAsync();
                            var storeInventoryItem = _storeInventoryService
                                .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId && x.StoreId == request.FromStoreId).FirstOrDefault();
                            if (storeInventoryItem != null)
                            {
                                storeInventoryItem.TotalQuantity -= request.Quantity;
                                storeInventoryItem.InOrderQuantity -= request.Quantity;
                                storeInventoryItem.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                var goodsNote = new GoodsDeliveryNote()
                                {
                                    Id = new Guid(),
                                    StoreId = request.FromStoreId,
                                    ReasonDescription = $"Chuyển hàng tới kho {toStoreName}",
                                    Total = 0,
                                    TotalByText = "Không đồng",
                                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),

                                };
                                await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                var goodsNoteDetail = new GoodsDeliveryNoteDetail()
                                {
                                    Id = new Guid(),
                                    GoodsDeliveryNoteId = goodsNote.Id,
                                    MaterialId = request.MaterialId,
                                    VariantId = request.VariantId,
                                    Quantity = request.Quantity,
                                    UnitPrice = 0,
                                    Total = 0
                                };
                                await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                await _goodsDeliveryNoteDetailService.SaveChangeAsync();
                                return Ok();
                            }

                        }
                        else
                        {
                            var warehouse = _warehouseService
                                .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                            var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                .FirstOrDefaultAsync();
                            if (warehouse != null)
                            {
                                warehouse.TotalQuantity -= request.Quantity;
                                warehouse.InRequestQuantity -= request.Quantity;
                                warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                var goodsNote = new GoodsDeliveryNote()
                                {
                                    Id = new Guid(),
                                    StoreId = null,
                                    ReasonDescription = $"Chuyển hàng tới kho {toStoreName}",
                                    Total = 0,
                                    TotalByText = "Không đồng",
                                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),

                                };
                                await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                var goodsNoteDetail = new GoodsDeliveryNoteDetail()
                                {
                                    Id = new Guid(),
                                    GoodsDeliveryNoteId = goodsNote.Id,
                                    MaterialId = request.MaterialId,
                                    VariantId = request.VariantId,
                                    Quantity = request.Quantity,
                                    UnitPrice = 0,
                                    Total = 0
                                };
                                await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                await _goodsDeliveryNoteDetailService.SaveChangeAsync();
                            }
                        }
                        await _storeInventoryService.SaveChangeAsync();

                    }
                    else
                    {
                        var variant = _variantService.Get(x => x.Id == request.VariantId).Include(x => x.ConversionUnit).FirstOrDefault();
                        if (variant != null)
                        {
                            if (variant.ConversionUnitId == null)
                            {
                                var storeInventory = _storeInventoryService
                                    .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id).FirstOrDefault();
                                if (storeInventory != null)
                                {
                                    storeInventory.TotalQuantity += request.Quantity;
                                    storeInventory.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                }
                                else
                                {
                                    await _storeInventoryService.AddAsync(new StoreInventory()
                                    {
                                        Id = Guid.NewGuid(),
                                        MaterialId = variant.MaterialId,
                                        VariantId = variant.Id,
                                        TotalQuantity = request.Quantity,
                                        LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                                    });
                                }
                                //var warehouse = _warehouseService
                                //    .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                                //if (warehouse != null)
                                //{
                                //    warehouse.TotalQuantity -= request.Quantity;
                                //    warehouse.InRequestQuantity -= request.Quantity;
                                //    warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                //}
                                if (request.FromStoreId != null)
                                {
                                    var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                        .FirstOrDefaultAsync();
                                    var storeInventoryItem = _storeInventoryService
                                        .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId && x.StoreId == request.FromStoreId).FirstOrDefault();
                                    if (storeInventoryItem != null)
                                    {
                                        storeInventoryItem.TotalQuantity -= request.Quantity;
                                        storeInventoryItem.InOrderQuantity -= request.Quantity;
                                        storeInventoryItem.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                        var goodsNote = new GoodsDeliveryNote()
                                        {
                                            Id = new Guid(),
                                            StoreId = request.FromStoreId,
                                            ReasonDescription = $"Chuyển hàng tới kho {toStoreName}",
                                            Total = 0,
                                            TotalByText = "Không đồng",
                                            TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),

                                        };
                                        await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                        var goodsNoteDetail = new GoodsDeliveryNoteDetail()
                                        {
                                            Id = new Guid(),
                                            GoodsDeliveryNoteId = goodsNote.Id,
                                            MaterialId = request.MaterialId,
                                            VariantId = request.VariantId,
                                            Quantity = request.Quantity,
                                            UnitPrice = 0,
                                            Total = 0
                                        };
                                        await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                        await _goodsDeliveryNoteDetailService.SaveChangeAsync();
                                        return Ok();
                                    }

                                }
                                else
                                {
                                    var warehouse = _warehouseService
                                        .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                                    var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                        .FirstOrDefaultAsync();
                                    if (warehouse != null)
                                    {
                                        warehouse.TotalQuantity -= request.Quantity;
                                        warehouse.InRequestQuantity -= request.Quantity;
                                        warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                        var goodsNote = new GoodsDeliveryNote()
                                        {
                                            Id = new Guid(),
                                            StoreId = null,
                                            ReasonDescription = $"Chuyển hàng tới kho {toStoreName}",
                                            Total = 0,
                                            TotalByText = "Không đồng",
                                            TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),

                                        };
                                        await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                        var goodsNoteDetail = new GoodsDeliveryNoteDetail()
                                        {
                                            Id = new Guid(),
                                            GoodsDeliveryNoteId = goodsNote.Id,
                                            MaterialId = request.MaterialId,
                                            VariantId = request.VariantId,
                                            Quantity = request.Quantity,
                                            UnitPrice = 0,
                                            Total = 0
                                        };
                                        await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                        await _goodsDeliveryNoteDetailService.SaveChangeAsync();
                                    }
                                }
                                await _storeInventoryService.SaveChangeAsync();
                            }
                            else
                            {
                                var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                    .FirstOrDefault();
                                if (rootVariant != null)
                                {
                                    var storeInventory = _storeInventoryService
                                        .Get(x => x.MaterialId == rootVariant.MaterialId && x.VariantId == rootVariant.Id).FirstOrDefault();
                                    if (storeInventory != null)
                                    {
                                        storeInventory.TotalQuantity += request.Quantity * variant.ConversionUnit.ConversionRate;
                                        storeInventory.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                    }
                                    else
                                    {
                                        await _storeInventoryService.AddAsync(new StoreInventory()
                                        {
                                            Id = Guid.NewGuid(),
                                            MaterialId = rootVariant.MaterialId,
                                            VariantId = rootVariant.Id,
                                            TotalQuantity = request.Quantity * variant.ConversionUnit.ConversionRate,
                                            LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                                        });
                                    }
                                    //var warehouse = _warehouseService
                                    //    .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                                    //if (warehouse != null)
                                    //{
                                    //    warehouse.TotalQuantity -= request.Quantity * variant.ConversionUnit.ConversionRate;
                                    //    warehouse.InRequestQuantity -= request.Quantity * variant.ConversionUnit.ConversionRate;
                                    //    warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                    //}
                                    if (request.FromStoreId != null)
                                    {
                                        var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                            .FirstOrDefaultAsync();
                                        var storeInventoryItem = _storeInventoryService
                                            .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId && x.StoreId == request.FromStoreId).FirstOrDefault();
                                        if (storeInventoryItem != null)
                                        {
                                            storeInventoryItem.TotalQuantity -= request.Quantity;
                                            storeInventoryItem.InOrderQuantity -= request.Quantity;
                                            storeInventoryItem.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                            var goodsNote = new GoodsDeliveryNote()
                                            {
                                                Id = new Guid(),
                                                StoreId = request.FromStoreId,
                                                ReasonDescription = $"Chuyển hàng tới kho {toStoreName}",
                                                Total = 0,
                                                TotalByText = "Không đồng",
                                                TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),

                                            };
                                            await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                            var goodsNoteDetail = new GoodsDeliveryNoteDetail()
                                            {
                                                Id = new Guid(),
                                                GoodsDeliveryNoteId = goodsNote.Id,
                                                MaterialId = request.MaterialId,
                                                VariantId = request.VariantId,
                                                Quantity = request.Quantity,
                                                UnitPrice = 0,
                                                Total = 0
                                            };
                                            await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                            await _goodsDeliveryNoteDetailService.SaveChangeAsync();
                                            return Ok();
                                        }

                                    }
                                    else
                                    {
                                        var warehouse = _warehouseService
                                            .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                                        var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                            .FirstOrDefaultAsync();
                                        if (warehouse != null)
                                        {
                                            warehouse.TotalQuantity -= request.Quantity;
                                            warehouse.InRequestQuantity -= request.Quantity;
                                            warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                            var goodsNote = new GoodsDeliveryNote()
                                            {
                                                Id = new Guid(),
                                                StoreId = null,
                                                ReasonDescription = $"Chuyển hàng tới kho {toStoreName}",
                                                Total = 0,
                                                TotalByText = "Không đồng",
                                                TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),

                                            };
                                            await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                            var goodsNoteDetail = new GoodsDeliveryNoteDetail()
                                            {
                                                Id = new Guid(),
                                                GoodsDeliveryNoteId = goodsNote.Id,
                                                MaterialId = request.MaterialId,
                                                VariantId = request.VariantId,
                                                Quantity = request.Quantity,
                                                UnitPrice = 0,
                                                Total = 0
                                            };
                                            await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                            await _goodsDeliveryNoteDetailService.SaveChangeAsync();
                                        }
                                    }
                                    await _storeInventoryService.SaveChangeAsync();
                                }
                            }
                        }

                    }
                }
                else
                {
                    if (request.Status != "Approved")
                        return BadRequest("The request status must be 'Approved'");
                    request.Status = "Canceled";
                    var item = await GetWarehouseItem(request.MaterialId, request.VariantId);
                    var conversionRate = await GetConversionRate(request.MaterialId, request.VariantId);
                    var importQuantity = conversionRate > 0 ? request.Quantity * conversionRate : request.Quantity;
                    item.InRequestQuantity -= importQuantity;
                }
                await _requestService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet("get-request-list-by-storeId-and-status")]
        public async Task<IActionResult> Get([FromQuery] string? storeId, [FromQuery] string? status)
        {
            try
            {
                var list = _requestService.Get(x => (storeId == null || x.StoreId.Equals(storeId)) && (status == null || x.Status.ToLower().Equals(status.ToLower()))).Include(x => x.Material).Include(x => x.Variant).Include(x => x.Store).Select(x => new
                {
                    x.Id,
                    store = x.Store.Name,
                    x.StoreId,
                    material = x.Material.Name,
                    materialCode = x.Material.MaterialCode,
                    x.MaterialId,
                    variant = x.Variant.SKU,
                    x.VariantId,
                    x.Status,
                    x.Quantity,
                    x.LastUpdateTime

                }).ToList();
                return Ok(new { data = list });
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        private async Task<Warehouse?> GetWarehouseItem(Guid materialId, Guid? variantId)
        {
            if (variantId == null)
                return await _warehouseService.Get(x => x.MaterialId == materialId && x.VariantId == variantId).FirstOrDefaultAsync();
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return await _warehouseService.Get(x => x.MaterialId == materialId && x.VariantId == variantId).FirstOrDefaultAsync();
                else
                {
                    return await _warehouseService.Get(x => x.VariantId == variant.AttributeVariantId).FirstOrDefaultAsync();
                }
            }
        }
        private async Task<StoreInventory?> GetStoreInventoryItem(Guid materialId, Guid? variantId, string fromStoreId)
        {
            if (variantId == null)
                return await _storeInventoryService.Get(x => x.MaterialId == materialId && x.VariantId == variantId && x.StoreId == fromStoreId).FirstOrDefaultAsync();
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return await _storeInventoryService.Get(x => x.MaterialId == materialId && x.VariantId == variantId && x.StoreId == fromStoreId).FirstOrDefaultAsync();
                else
                {
                    return await _storeInventoryService.Get(x => x.VariantId == variant.AttributeVariantId && x.StoreId == fromStoreId).FirstOrDefaultAsync();
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
    }
}
