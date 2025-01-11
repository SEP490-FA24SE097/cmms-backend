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
using CMMS.Core.Enums;
using CMMS.Infrastructure.Handlers;

namespace CMMS.API.Controllers
{
    [HasPermission(Infrastructure.Enums.PermissionName.ImportRequestPermissions)]
    [Route("api/store-material-import-requests")]
    [ApiController]
    public class StoreMaterialImportRequestController : ControllerBase
    {
        private readonly IStoreMaterialImportRequestService _requestService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IWarehouseService _warehouseService;
        private readonly IVariantService _variantService;
        private readonly IGoodsNoteService _goodsDeliveryNoteService;
        private readonly IGoodsNoteDetailService _goodsDeliveryNoteDetailService;
        private readonly IStoreService _storeService;

        public StoreMaterialImportRequestController(IStoreService storeService, IGoodsNoteService goodsDeliveryNoteService, IGoodsNoteDetailService goodsDeliveryNoteDetailService, IWarehouseService warehouseService, IStoreMaterialImportRequestService requestService, IStoreInventoryService storeInventoryService, IVariantService variantService)
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
                    Id = Guid.NewGuid(),
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
        public async Task<IActionResult> Create([FromBody] CancelDTO dto)
        {
            try
            {
                var request = await _requestService.FindAsync(dto.RequestId);
                if (request.Status != "Processing")
                {
                    return BadRequest("Trạng thái yêu cầu phải là 'Processing'");
                }
                request.Status = "Canceled";
                request.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                await _requestService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }


        [HttpPost("approve-or-cancel-store-material-import-request")]
        public async Task<IActionResult> Create([FromBody] ApproveDTO dto)
        {
            try
            {
                var request = await _requestService.FindAsync(dto.RequestId);
                var item = await GetWarehouseItem(request.MaterialId, request.VariantId);
                var conversionRate = await GetConversionRate(request.MaterialId, request.VariantId);
                var importQuantity = conversionRate > 0 ? request.Quantity * conversionRate : request.Quantity;
                if (dto.IsApproved)
                {
                    if (request.Status != "Processing")
                    {
                        return BadRequest("Trạng thái yêu cầu phải là 'Processing'");
                    }
                    if (!dto.FromStoreId.IsNullOrEmpty())
                    {
                        if (dto.FromStoreId == request.StoreId)
                        {
                            return BadRequest("From store id can not be store id!");
                        }
                        var storeInventoryItem = await GetStoreInventoryItem(request.MaterialId, request.VariantId, dto.FromStoreId);
                        if (storeInventoryItem == null || storeInventoryItem.TotalQuantity - (storeInventoryItem.InOrderQuantity ?? 0) < importQuantity)
                            return BadRequest("Không đủ sản phẩm trong kho cửa hàng để chuyển");
                        storeInventoryItem.InOrderQuantity = (storeInventoryItem.InOrderQuantity ?? 0) + importQuantity;
                        request.FromStoreId = dto.FromStoreId;
                        request.Status = "Approved";
                        request.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                        await _requestService.SaveChangeAsync();
                        return Ok();
                    }
                    if (item == null || item.TotalQuantity - (item.InRequestQuantity ?? 0) < importQuantity)
                        return BadRequest("Không đủ sản phẩm trong kho");
                    item.InRequestQuantity = (item.InRequestQuantity ?? 0) + importQuantity;
                    request.Status = "Approved";
                }
                else
                {
                    if (request.Status != "Processing")
                    {
                        return BadRequest("Trạng thái yêu cầu phải là 'Processing'");
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
        public async Task<IActionResult> Confirm([FromBody] ConfirmDTO dto)
        {
            try
            {
                var request = await _requestService.FindAsync(dto.RequestId);

                if (dto.IsConfirmed)
                {
                    if (request.Status != "Approved")
                    {
                        return BadRequest("Trạng thái yêu cầu phải là 'Approved'");
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
                                StoreId = request.StoreId,
                                MaterialId = request.MaterialId,
                                VariantId = request.Id,
                                TotalQuantity = request.Quantity,
                                MinStock = 10,
                                MaxStock = 1000,
                                LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                            });
                        }

                        if (request.FromStoreId != null)
                        {
                            var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                .FirstOrDefaultAsync();
                            var fromStoreName = await _storeService.Get(x => x.Id == request.FromStoreId).Select(x => x.Name)
                                .FirstOrDefaultAsync();
                            var storeInventoryItem = _storeInventoryService
                                .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId && x.StoreId == request.FromStoreId).FirstOrDefault();
                            if (storeInventoryItem != null)
                            {
                                storeInventoryItem.TotalQuantity -= request.Quantity;
                                storeInventoryItem.InOrderQuantity = (storeInventoryItem.InOrderQuantity ?? 0) - request.Quantity;
                                storeInventoryItem.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                var goodsNote = new GoodsNote()
                                {
                                    Id = Guid.NewGuid(),
                                    StoreId = request.FromStoreId,
                                    ReasonDescription = $"Chuyển hàng từ kho {fromStoreName} tới kho {toStoreName}",
                                    TotalQuantity = request.Quantity,
                                    Type = (int)GoodsNoteType.Delivery,
                                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                };
                                await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                var goodsNoteDetail = new GoodsNoteDetail()
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsNoteId = goodsNote.Id,
                                    MaterialId = request.MaterialId,
                                    VariantId = request.VariantId,
                                    Quantity = request.Quantity,
                                };
                                await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                var secondGoodsNote = new GoodsNote()
                                {
                                    Id = Guid.NewGuid(),
                                    StoreId = request.StoreId,
                                    ReasonDescription = $"Nhập hàng từ kho {fromStoreName} tới kho {toStoreName}",
                                    TotalQuantity = request.Quantity,
                                    Type = (int)GoodsNoteType.Receive,
                                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                };
                                await _goodsDeliveryNoteService.AddAsync(secondGoodsNote);
                                var secondGoodsNoteDetail = new GoodsNoteDetail()
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsNoteId = goodsNote.Id,
                                    MaterialId = request.MaterialId,
                                    VariantId = request.VariantId,
                                    Quantity = request.Quantity,
                                };
                                await _goodsDeliveryNoteDetailService.AddAsync(secondGoodsNoteDetail);
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
                                warehouse.InRequestQuantity = (warehouse.InRequestQuantity ?? 0) - request.Quantity;
                                warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                var goodsNote = new GoodsNote()
                                {
                                    Id = Guid.NewGuid(),
                                    StoreId = request.FromStoreId,
                                    ReasonDescription = $"Chuyển hàng từ kho tổng tới kho {toStoreName}",
                                    TotalQuantity = request.Quantity,
                                    Type = (int)GoodsNoteType.Delivery,
                                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                };
                                await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                var goodsNoteDetail = new GoodsNoteDetail()
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsNoteId = goodsNote.Id,
                                    MaterialId = request.MaterialId,
                                    VariantId = request.VariantId,
                                    Quantity = request.Quantity,
                                };
                                await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                var secondGoodsNote = new GoodsNote()
                                {
                                    Id = Guid.NewGuid(),
                                    StoreId = request.StoreId,
                                    ReasonDescription = $"Nhập hàng từ kho tổng tới kho {toStoreName}",
                                    TotalQuantity = request.Quantity,
                                    Type = (int)GoodsNoteType.Receive,
                                    TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                };
                                await _goodsDeliveryNoteService.AddAsync(secondGoodsNote);
                                var secondGoodsNoteDetail = new GoodsNoteDetail()
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsNoteId = secondGoodsNote.Id,
                                    MaterialId = request.MaterialId,
                                    VariantId = request.VariantId,
                                    Quantity = request.Quantity,
                                };
                                await _goodsDeliveryNoteDetailService.AddAsync(secondGoodsNoteDetail);
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
                                        StoreId = request.StoreId,
                                        MaterialId = variant.MaterialId,
                                        VariantId = variant.Id,
                                        TotalQuantity = request.Quantity,
                                        MinStock = 10,
                                        MaxStock = 1000,
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
                                    var fromStoreName = await _storeService.Get(x => x.Id == request.FromStoreId).Select(x => x.Name)
                                        .FirstOrDefaultAsync();
                                    var storeInventoryItem = _storeInventoryService
                                        .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId && x.StoreId == request.FromStoreId).FirstOrDefault();
                                    if (storeInventoryItem != null)
                                    {
                                        storeInventoryItem.TotalQuantity -= request.Quantity;
                                        storeInventoryItem.InOrderQuantity = (storeInventoryItem.InOrderQuantity ?? 0) - request.Quantity;
                                        storeInventoryItem.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                        var goodsNote = new GoodsNote()
                                        {
                                            Id = Guid.NewGuid(),
                                            StoreId = request.FromStoreId,
                                            ReasonDescription = $"Chuyển hàng từ kho {fromStoreName} tới kho {toStoreName}",
                                            TotalQuantity = request.Quantity,
                                            Type = (int)GoodsNoteType.Delivery,
                                            TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                        };
                                        await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                        var goodsNoteDetail = new GoodsNoteDetail()
                                        {
                                            Id = Guid.NewGuid(),
                                            GoodsNoteId = goodsNote.Id,
                                            MaterialId = request.MaterialId,
                                            VariantId = request.VariantId,
                                            Quantity = request.Quantity,
                                        };
                                        await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                        var secondGoodsNote = new GoodsNote()
                                        {
                                            Id = Guid.NewGuid(),
                                            StoreId = request.StoreId,
                                            ReasonDescription = $"Nhập hàng từ kho {fromStoreName} tới kho {toStoreName}",
                                            TotalQuantity = request.Quantity,
                                            Type = (int)GoodsNoteType.Receive,
                                            TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                        };
                                        await _goodsDeliveryNoteService.AddAsync(secondGoodsNote);
                                        var secondGoodsNoteDetail = new GoodsNoteDetail()
                                        {
                                            Id = Guid.NewGuid(),
                                            GoodsNoteId = goodsNote.Id,
                                            MaterialId = request.MaterialId,
                                            VariantId = request.VariantId,
                                            Quantity = request.Quantity,
                                        };
                                        await _goodsDeliveryNoteDetailService.AddAsync(secondGoodsNoteDetail);
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
                                        warehouse.InRequestQuantity = (warehouse.InRequestQuantity ?? 0) - request.Quantity;
                                        warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                        var goodsNote = new GoodsNote()
                                        {
                                            Id = Guid.NewGuid(),
                                            StoreId = request.FromStoreId,
                                            ReasonDescription = $"Chuyển hàng từ kho tổng tới kho {toStoreName}",
                                            TotalQuantity = request.Quantity,
                                            Type = (int)GoodsNoteType.Delivery,
                                            TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                        };
                                        await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                        var goodsNoteDetail = new GoodsNoteDetail()
                                        {
                                            Id = Guid.NewGuid(),
                                            GoodsNoteId = goodsNote.Id,
                                            MaterialId = request.MaterialId,
                                            VariantId = request.VariantId,
                                            Quantity = request.Quantity,
                                        };
                                        await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                        var secondGoodsNote = new GoodsNote()
                                        {
                                            Id = Guid.NewGuid(),
                                            StoreId = request.StoreId,
                                            ReasonDescription = $"Nhập hàng từ kho tổng tới kho {toStoreName}",
                                            TotalQuantity = request.Quantity,
                                            Type = (int)GoodsNoteType.Receive,
                                            TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                        };
                                        await _goodsDeliveryNoteService.AddAsync(secondGoodsNote);
                                        var secondGoodsNoteDetail = new GoodsNoteDetail()
                                        {
                                            Id = Guid.NewGuid(),
                                            GoodsNoteId = goodsNote.Id,
                                            MaterialId = request.MaterialId,
                                            VariantId = request.VariantId,
                                            Quantity = request.Quantity,
                                        };
                                        await _goodsDeliveryNoteDetailService.AddAsync(secondGoodsNoteDetail);
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
                                    if (storeInventory == null)
                                    {
                                        await _storeInventoryService.AddAsync(new StoreInventory()
                                        {
                                            Id = Guid.NewGuid(),
                                            StoreId = request.StoreId,
                                            MaterialId = rootVariant.MaterialId,
                                            VariantId = rootVariant.Id,
                                            TotalQuantity = request.Quantity * variant.ConversionUnit.ConversionRate,
                                            MinStock = 10,
                                            MaxStock = 1000,
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
                                        var fromStoreName = await _storeService.Get(x => x.Id == request.FromStoreId).Select(x => x.Name)
                                            .FirstOrDefaultAsync();
                                        var storeInventoryItem = _storeInventoryService
                                            .Get(x => x.MaterialId == request.MaterialId && x.VariantId == variant.AttributeVariantId && x.StoreId == request.FromStoreId).FirstOrDefault();
                                        if (storeInventoryItem != null)
                                        {
                                            storeInventoryItem.TotalQuantity -= request.Quantity * variant.ConversionUnit.ConversionRate;
                                            storeInventoryItem.InOrderQuantity = (storeInventoryItem.InOrderQuantity ?? 0) - (request.Quantity * variant.ConversionUnit.ConversionRate);
                                            storeInventoryItem.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                            var goodsNote = new GoodsNote()
                                            {
                                                Id = Guid.NewGuid(),
                                                StoreId = request.FromStoreId,
                                                ReasonDescription = $"Chuyển hàng từ kho {fromStoreName} tới kho {toStoreName}",
                                                TotalQuantity = request.Quantity,
                                                Type = (int)GoodsNoteType.Delivery,
                                                TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                            };
                                            await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                            var goodsNoteDetail = new GoodsNoteDetail()
                                            {
                                                Id = Guid.NewGuid(),
                                                GoodsNoteId = goodsNote.Id,
                                                MaterialId = request.MaterialId,
                                                VariantId = request.VariantId,
                                                Quantity = request.Quantity,
                                            };
                                            await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                            var secondGoodsNote = new GoodsNote()
                                            {
                                                Id = Guid.NewGuid(),
                                                StoreId = request.StoreId,
                                                ReasonDescription = $"Nhập hàng từ kho {fromStoreName} tới kho {toStoreName}",
                                                TotalQuantity = request.Quantity,
                                                Type = (int)GoodsNoteType.Receive,
                                                TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                            };
                                            await _goodsDeliveryNoteService.AddAsync(secondGoodsNote);
                                            var secondGoodsNoteDetail = new GoodsNoteDetail()
                                            {
                                                Id = Guid.NewGuid(),
                                                GoodsNoteId = goodsNote.Id,
                                                MaterialId = request.MaterialId,
                                                VariantId = request.VariantId,
                                                Quantity = request.Quantity,
                                            };
                                            await _goodsDeliveryNoteDetailService.AddAsync(secondGoodsNoteDetail);
                                            await _goodsDeliveryNoteDetailService.SaveChangeAsync();
                                            return Ok();
                                        }

                                    }
                                    else
                                    {
                                        var warehouse = _warehouseService
                                            .Get(x => x.MaterialId == request.MaterialId && x.VariantId == variant.AttributeVariantId).FirstOrDefault();
                                        var toStoreName = await _storeService.Get(x => x.Id == request.StoreId).Select(x => x.Name)
                                            .FirstOrDefaultAsync();
                                       
                                        if (warehouse != null)
                                        {
                                            warehouse.TotalQuantity -= request.Quantity * variant.ConversionUnit.ConversionRate;
                                            warehouse.InRequestQuantity = (warehouse.InRequestQuantity ?? 0) - (request.Quantity * variant.ConversionUnit.ConversionRate);
                                            warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
                                            var goodsNote = new GoodsNote()
                                            {
                                                Id = Guid.NewGuid(),
                                                StoreId = request.FromStoreId,
                                                ReasonDescription = $"Chuyển hàng từ kho tổng tới kho {toStoreName}",
                                                TotalQuantity = request.Quantity,
                                                Type = (int)GoodsNoteType.Delivery,
                                                TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                            };
                                            await _goodsDeliveryNoteService.AddAsync(goodsNote);
                                            var goodsNoteDetail = new GoodsNoteDetail()
                                            {
                                                Id = Guid.NewGuid(),
                                                GoodsNoteId = goodsNote.Id,
                                                MaterialId = request.MaterialId,
                                                VariantId = request.VariantId,
                                                Quantity = request.Quantity,
                                            };
                                            await _goodsDeliveryNoteDetailService.AddAsync(goodsNoteDetail);
                                            var secondGoodsNote = new GoodsNote()
                                            {
                                                Id = Guid.NewGuid(),
                                                StoreId = request.StoreId,
                                                ReasonDescription = $"Nhập hàng từ kho tổng tới kho {toStoreName}",
                                                TotalQuantity = request.Quantity,
                                                Type = (int)GoodsNoteType.Receive,
                                                TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                                            };
                                            await _goodsDeliveryNoteService.AddAsync(secondGoodsNote);
                                            var secondGoodsNoteDetail = new GoodsNoteDetail()
                                            {
                                                Id = Guid.NewGuid(),
                                                GoodsNoteId = goodsNote.Id,
                                                MaterialId = request.MaterialId,
                                                VariantId = request.VariantId,
                                                Quantity = request.Quantity,
                                            };
                                            await _goodsDeliveryNoteDetailService.AddAsync(secondGoodsNoteDetail);
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
                        return BadRequest("Trạng thái yêu cầu phải là 'Approved'");
                    request.Status = "Canceled";
                    var item = await GetWarehouseItem(request.MaterialId, request.VariantId);
                    var conversionRate = await GetConversionRate(request.MaterialId, request.VariantId);
                    var importQuantity = conversionRate > 0 ? request.Quantity * conversionRate : request.Quantity;
                    item.InRequestQuantity = (item.InRequestQuantity ?? 0) - importQuantity;
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
        public async Task<IActionResult> Get([FromQuery] int? page, [FromQuery] int? itemPerPage, [FromQuery] string? storeId, [FromQuery] string? status)
        {
            try
            {
                var list = _requestService.Get(x => (storeId == null || x.StoreId.Equals(storeId)) && (status == null || x.Status.ToLower().Equals(status.ToLower()))).Include(x => x.Material).Include(x => x.Variant).Include(x => x.Store).Select(x => new
                {
                    x.Id,
                    requestCode = "REQ-" + x.Id.ToString().ToUpper().Substring(0, 4),
                    store = x.Store.Name,
                    x.StoreId,
                    material = x.Material.Name,
                    materialCode = x.Material.MaterialCode,
                    x.MaterialId,
                    variant = x.Variant == null ? null : x.Variant.SKU,
                    x.VariantId,
                    x.Status,
                    x.Quantity,
                    x.LastUpdateTime

                }).OrderByDescending(x => x.LastUpdateTime).ToList();
                var secondResult = Helpers.LinqHelpers.ToPageList(list, page == null ? 0 : (int)page - 1,
                    itemPerPage == null ? 12 : (int)itemPerPage);
                return Ok(new
                {
                    data = secondResult,
                    pagination = new
                    {
                        total = list.Count,
                        perPage = itemPerPage == null ? 12 : itemPerPage,
                        currentPage = page == null ? 1 : page
                    }
                });
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
