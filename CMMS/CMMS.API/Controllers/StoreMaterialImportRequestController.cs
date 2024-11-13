using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [Route("api/store-material-import-requests")]
    [ApiController]
    public class StoreMaterialImportRequestController : ControllerBase
    {
        private readonly IStoreMaterialImportRequestService _requestService;
        private readonly IStoreInventoryService _storeInventoryService;

        public StoreMaterialImportRequestController(IStoreMaterialImportRequestService requestService, IStoreInventoryService storeInventoryService)
        {
            _storeInventoryService = storeInventoryService;
            _requestService = requestService;

        }

        [HttpPost("create-store-material-import-request")]
        public async Task<IActionResult> Create([FromBody] ImportRequestCM importRequest)
        {
            try
            {
                await _requestService.AddAsync(new StoreMaterialImportRequest
                {
                    Id = new Guid(),
                    StoreId = importRequest.StoreId,
                    MaterialId = importRequest.MaterialId,
                    VariantId = importRequest.VariantId,
                    Quantity = importRequest.Quantity,
                    Status = "Processing",
                    LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime()
                });
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
        [HttpPost("confirm-store-material-import-request")]
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
                    var material = _storeInventoryService
                        .Get(x => x.MaterialId == request.MaterialId && x.VariantId == request.VariantId).FirstOrDefault();
                    if (material != null)
                    {
                        material.TotalQuantity += request.Quantity;
                    }
                    else
                    {
                        _storeInventoryService.AddAsync(new StoreInventory
                        {
                            Id = new Guid(),
                            TotalQuantity = request.Quantity,
                            MaterialId = request.MaterialId,
                            VariantId = request.VariantId,
                            MinStock = 0,
                            MaxStock = 10000,
                            LastUpdateTime = TimeConverter.TimeConverter.GetVietNamTime(),
                            StoreId = request.StoreId
                        });
                    }
                    await _storeInventoryService.SaveChangeAsync();
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
