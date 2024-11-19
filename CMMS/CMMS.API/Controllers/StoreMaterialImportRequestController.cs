using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CMMS.API.Controllers
{
    [Route("api/store-material-import-requests")]
    [ApiController]
    public class StoreMaterialImportRequestController : ControllerBase
    {
        private readonly IStoreMaterialImportRequestService _requestService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IVariantService _variantService;

        public StoreMaterialImportRequestController(IStoreMaterialImportRequestService requestService, IStoreInventoryService storeInventoryService, IVariantService variantService)
        {
            _storeInventoryService = storeInventoryService;
            _requestService = requestService;
            _variantService = variantService;

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
        [HttpPost("approve-or-cancel-store-material-import-request")]
        public async Task<IActionResult> Create([FromQuery] Guid requestId, [FromQuery] bool isApproved)
        {
            try
            {
                var request = await _requestService.FindAsync(requestId);
                if (isApproved)
                {
                    if (request.Status != "Processing")
                    {
                        return BadRequest();
                    }
                    request.Status = "Approved";
                }
                else
                {
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
                        return BadRequest();
                    }
                    request.Status = "Confirmed";
                    if (request.VariantId == null)
                    {
                        var warehouse = _storeInventoryService
                            .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                        if (warehouse != null)
                        {
                            warehouse.TotalQuantity += request.Quantity;
                            warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
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
                        await _storeInventoryService.SaveChangeAsync();

                    }
                    else
                    {
                        var variant = _variantService.Get(x => x.Id == request.VariantId).Include(x => x.ConversionUnit).FirstOrDefault();
                        if (variant != null)
                        {
                            if (variant.ConversionUnitId == null)
                            {
                                var warehouse = _storeInventoryService
                                    .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id).FirstOrDefault();
                                if (warehouse != null)
                                {
                                    warehouse.TotalQuantity += request.Quantity;
                                    warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
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
                                await _storeInventoryService.SaveChangeAsync();
                            }
                            else
                            {
                                var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                    .FirstOrDefault();
                                if (rootVariant != null)
                                {
                                    var warehouse = _storeInventoryService
                                        .Get(x => x.MaterialId == rootVariant.MaterialId && x.VariantId == rootVariant.Id).FirstOrDefault();
                                    if (warehouse != null)
                                    {
                                        warehouse.TotalQuantity += request.Quantity * variant.ConversionUnit.ConversionRate;
                                        warehouse.LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime();
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
                                    await _storeInventoryService.SaveChangeAsync();
                                }
                            }
                        }

                    }
                }
                else
                {
                    request.Status = "Canceled";
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
                var list = _requestService.Get(x => (storeId == null || x.StoreId.Equals(storeId)) && (status == null || x.Status.Equals(status))).Include(x => x.Material).Include(x => x.Variant).Include(x => x.Store).Select(x => new
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
    }
}
