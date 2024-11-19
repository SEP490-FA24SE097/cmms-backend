using CMMS.Core.Entities;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMMS.Core.Models;
using static CMMS.API.TimeConverter.TimeConverter;
using CMMS.API.TimeConverter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/imports")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _importService;
        private readonly IWarehouseService _warehouseService;
        private readonly IImportDetailService _importDetailService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;

        public ImportController(IImportService importService, IWarehouseService warehouseService, IImportDetailService importDetailService, IVariantService variantService, IMaterialVariantAttributeService materialVariantAttributeService, IMaterialService materialService)
        {
            _importService = importService;
            _warehouseService = warehouseService;
            _importDetailService = importDetailService;
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _materialService = materialService;
        }

        // GET: api/imports
        [HttpGet]
        public IActionResult GetAll([FromQuery] int page, [FromQuery] int itemPerPage)
        {
            try
            {
                return Ok(new
                {
                    data = Helpers.LinqHelpers.ToPageList(_importService.GetAll().ToList(), page - 1, itemPerPage),
                    pagination = new
                    {
                        total = _importService.GetAll().ToList().Count,
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

        // GET: api/imports/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var import = await _importService.FindAsync(id);
            if (import == null)
            {
                return NotFound();
            }
            return Ok(import);
        }

        // POST: api/imports
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ImportCM import)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var imp = new Import
                {
                    Id = Guid.NewGuid(),
                    Quantity = import.Quantity,
                    TotalPrice = import.TotalPrice,
                    TimeStamp = GetVietNamTime(),
                    SupplierId = import.SupplierId,
                    TotalDiscount = import.TotalDiscount,
                    TotalDue = import.TotalDue,
                    TotalPaid = import.TotalPaid,
                    Note = import.Note,
                    Status = import.IsCompleted ? "Đã nhập hàng" : "Phiếu tạm"
                };
                await _importService.AddAsync(imp);
                await _importService.SaveChangeAsync();
                await _importDetailService.AddRange(import.ImportDetails.Select(x => new ImportDetail()
                {
                    Id = new Guid(),
                    ImportId = imp.Id,
                    VariantId = x.VariantId,
                    MaterialId = x.MaterialId,
                    PriceAfterDiscount = x.PriceAfterDiscount,
                    DiscountPrice = x.DiscountPrice,
                    UnitDiscount = x.UnitDiscount,
                    UnitPrice = x.UnitPrice,
                    Quantity = x.Quantity,
                    Note = x.Note
                }));
                await _importDetailService.SaveChangeAsync();

                if (import.IsCompleted)
                {
                    var list = _importService.Get(x => x.Id == imp.Id).Include(x => x.ImportDetails).Select(x => x.ImportDetails).FirstOrDefault();
                    foreach (var item in list)
                    {
                        if (item.VariantId == null)
                        {
                            var warehouse = _warehouseService
                                .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId).FirstOrDefault();
                            if (warehouse != null)
                            {
                                warehouse.TotalQuantity += item.Quantity;
                                warehouse.LastUpdateTime = GetVietNamTime();
                            }
                            else
                            {
                                await _warehouseService.AddAsync(new Warehouse
                                {
                                    Id = Guid.NewGuid(),
                                    MaterialId = item.MaterialId,
                                    VariantId = item.Id,
                                    TotalQuantity = item.Quantity,
                                    LastUpdateTime = GetVietNamTime()
                                });
                            }
                            await _warehouseService.SaveChangeAsync();

                        }
                        else
                        {
                            var variant = _variantService.Get(x => x.Id == item.VariantId).Include(x => x.ConversionUnit).FirstOrDefault();
                            if (variant != null)
                            {
                                if (variant.ConversionUnitId == null)
                                {
                                    var warehouse = _warehouseService
                                        .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id).FirstOrDefault();
                                    if (warehouse != null)
                                    {
                                        warehouse.TotalQuantity += item.Quantity;
                                        warehouse.LastUpdateTime = GetVietNamTime();
                                    }
                                    else
                                    {
                                        await _warehouseService.AddAsync(new Warehouse
                                        {
                                            Id = Guid.NewGuid(),
                                            MaterialId = variant.MaterialId,
                                            VariantId = variant.Id,
                                            TotalQuantity = item.Quantity,
                                            LastUpdateTime = GetVietNamTime()
                                        });
                                    }
                                    await _warehouseService.SaveChangeAsync();
                                }
                                else
                                {
                                    var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                        .FirstOrDefault();
                                    if (rootVariant != null)
                                    {
                                        var warehouse = _warehouseService
                                            .Get(x => x.MaterialId == rootVariant.MaterialId && x.VariantId == rootVariant.Id).FirstOrDefault();
                                        if (warehouse != null)
                                        {
                                            warehouse.TotalQuantity += item.Quantity * variant.ConversionUnit.ConversionRate;
                                            warehouse.LastUpdateTime = GetVietNamTime();
                                        }
                                        else
                                        {
                                            await _warehouseService.AddAsync(new Warehouse
                                            {
                                                Id = Guid.NewGuid(),
                                                MaterialId = rootVariant.MaterialId,
                                                VariantId = rootVariant.Id,
                                                TotalQuantity = item.Quantity * variant.ConversionUnit.ConversionRate,
                                                LastUpdateTime = GetVietNamTime()
                                            });
                                        }
                                        await _warehouseService.SaveChangeAsync();
                                    }
                                }
                            }

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

        [HttpPost("complete-import")]
        public async Task<IActionResult> CompleteImport([FromQuery] Guid importId)
        {
            try
            {
                var list = _importService.Get(x => x.Id == importId).Include(x => x.ImportDetails)
                    .Select(x => x.ImportDetails).FirstOrDefault();
                foreach (var item in list)
                {
                    if (item.VariantId == null)
                    {
                        var warehouse = _warehouseService
                            .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId).FirstOrDefault();
                        if (warehouse != null)
                        {
                            warehouse.TotalQuantity += item.Quantity;
                            warehouse.LastUpdateTime = GetVietNamTime();
                        }
                        else
                        {
                            await _warehouseService.AddAsync(new Warehouse
                            {
                                Id = Guid.NewGuid(),
                                MaterialId = item.MaterialId,
                                VariantId = item.Id,
                                TotalQuantity = item.Quantity,
                                LastUpdateTime = GetVietNamTime()
                            });
                        }
                        await _warehouseService.SaveChangeAsync();

                    }
                    else
                    {
                        var variant = _variantService.Get(x => x.Id == item.VariantId).Include(x => x.ConversionUnit).FirstOrDefault();
                        if (variant != null)
                        {
                            if (variant.ConversionUnitId == null)
                            {
                                var warehouse = _warehouseService
                                    .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id).FirstOrDefault();
                                if (warehouse != null)
                                {
                                    warehouse.TotalQuantity += item.Quantity;
                                    warehouse.LastUpdateTime = GetVietNamTime();
                                }
                                else
                                {
                                    await _warehouseService.AddAsync(new Warehouse
                                    {
                                        Id = Guid.NewGuid(),
                                        MaterialId = variant.MaterialId,
                                        VariantId = variant.Id,
                                        TotalQuantity = item.Quantity,
                                        LastUpdateTime = GetVietNamTime()
                                    });
                                }
                                await _warehouseService.SaveChangeAsync();
                            }
                            else
                            {
                                var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                    .FirstOrDefault();
                                if (rootVariant != null)
                                {
                                    var warehouse = _warehouseService
                                        .Get(x => x.MaterialId == rootVariant.MaterialId && x.VariantId == rootVariant.Id).FirstOrDefault();
                                    if (warehouse != null)
                                    {
                                        warehouse.TotalQuantity += item.Quantity * variant.ConversionUnit.ConversionRate;
                                        warehouse.LastUpdateTime = GetVietNamTime();
                                    }
                                    else
                                    {
                                        await _warehouseService.AddAsync(new Warehouse
                                        {
                                            Id = Guid.NewGuid(),
                                            MaterialId = rootVariant.MaterialId,
                                            VariantId = rootVariant.Id,
                                            TotalQuantity = item.Quantity * variant.ConversionUnit.ConversionRate,
                                            LastUpdateTime = GetVietNamTime()
                                        });
                                    }
                                    await _warehouseService.SaveChangeAsync();
                                }
                            }
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
