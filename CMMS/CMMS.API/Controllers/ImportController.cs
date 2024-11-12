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
using Microsoft.IdentityModel.Tokens;

namespace CMMS.API.Controllers
{
    [Route("api/imports")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _importService;
        private readonly IWarehouseService _warehouseService;

        public ImportController(IImportService importService, IWarehouseService warehouseService)
        {
            _importService = importService;
            _warehouseService = warehouseService;
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

                await _importService.AddAsync(new Import
                {
                    Id = Guid.NewGuid(),
                    MaterialId = import.MaterialId,
                    VariantId = import.VariantId,
                    Quantity = import.Quantity,
                    TotalPrice = import.TotalPrice,
                    TimeStamp = GetVietNamTime(),
                    SupplierId = import.SupplierId
                });
                await _importService.SaveChangeAsync();
                var warehouse = _warehouseService
                    .Get(x => x.MaterialId == import.MaterialId && x.VariantId == import.VariantId).FirstOrDefault();
                if (warehouse != null)
                {
                    warehouse.TotalQuantity += import.Quantity;
                    warehouse.LastUpdateTime = GetVietNamTime();
                }
                else
                {
                    await _warehouseService.AddAsync(new Warehouse
                    {
                        Id = Guid.NewGuid(),
                        MaterialId = import.MaterialId,
                        VariantId = import.VariantId,
                        TotalQuantity = import.Quantity,
                        LastUpdateTime = GetVietNamTime()
                    });
                }
                await _warehouseService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }

       
    }
}
