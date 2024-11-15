﻿using CMMS.Core.Entities;
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
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;

        public ImportController(IImportService importService, IWarehouseService warehouseService, IImportDetailService importDetailService, IVariantService variantService, IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _importService = importService;
            _warehouseService = warehouseService;
            _importDetailService = importDetailService;
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;
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
                        var warehouse = _warehouseService
                            .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId).FirstOrDefault();
                        if (item.VariantId == null)
                        {
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
                                    VariantId = item.VariantId,
                                    TotalQuantity = item.Quantity,
                                    LastUpdateTime = GetVietNamTime()
                                });
                            }
                            await _warehouseService.SaveChangeAsync();
                        }
                        else
                        {
                            var attributeVariantCheck =
                                _variantService
                                    .Get(x => x.Id == item.VariantId && x.MaterialVariantAttributes.Count > 0)
                                    .Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                            if (attributeVariantCheck != null)
                            {

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

        //[HttpPost]
        //public async Task<IActionResult> CompleteImport([FromQuery] Guid importId)
        //{
        //    var list = _importService.Get(x => x.Id == importId).Include(x => x.ImportDetails).Select(x => x.ImportDetails).FirstOrDefault();
        //    foreach (var item in list)
        //    {
        //        var warehouse = _warehouseService
        //            .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId).FirstOrDefault();
        //        if (warehouse != null)
        //        {
        //            warehouse.TotalQuantity += item.Quantity;
        //            warehouse.LastUpdateTime = GetVietNamTime();
        //        }
        //        else
        //        {
        //            await _warehouseService.AddAsync(new Warehouse
        //            {
        //                Id = Guid.NewGuid(),
        //                MaterialId = item.MaterialId,
        //                VariantId = item.VariantId,
        //                TotalQuantity = item.Quantity,
        //                LastUpdateTime = GetVietNamTime()
        //            });
        //        }
        //    }
        //}

    }
}
