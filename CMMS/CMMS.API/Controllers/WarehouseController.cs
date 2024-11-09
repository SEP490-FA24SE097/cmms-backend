using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [Route("api/warehouse")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {

        private readonly IWarehouseService _warehouseService;
        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }
        [HttpGet("get-warehouse-products")]
        public async Task<IActionResult> Get([FromQuery] int page, [FromQuery] int itemPerPage)
        {
            try
            {
                var items = await _warehouseService.GetAll().Include(x => x.Material).Include(x => x.Variant).Select(x => new
                {
                    x.Id,
                    x.MaterialId,
                    MaterialName = x.Material.Name,
                    MaterialImage = x.Material.ImageUrl,
                    x.VariantId,
                    VariantName = x.Variant == null ? null : x.Variant.SKU,
                    VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
                    Quantity = x.TotalQuantity,
                    x.LastUpdateTime
                }).ToListAsync();
                var result = Helpers.LinqHelpers.ToPageList(items, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = items.Count,
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
        [HttpPost("search-and-filter")]
        public async Task<IActionResult> Get(SAFProductsDTO safProductsDto, [FromQuery] int page, [FromQuery] int itemPerPage)
        {
            try
            {
                var items = await _warehouseService.Get(x =>
                    x.Material.Name.Contains(safProductsDto.NameKeyWord)
                    && (safProductsDto.BrandId == null || x.Material.BrandId == safProductsDto.BrandId)
                    && (safProductsDto.CategoryId == null || x.Material.CategoryId == safProductsDto.CategoryId)
                ).Include(x => x.Material).Include(x => x.Variant).Select(x => new
                {
                    x.Id,
                    x.MaterialId,
                    MaterialName = x.Material.Name,
                    MaterialImage = x.Material.ImageUrl,
                    x.VariantId,
                    VariantName = x.Variant == null ? null : x.Variant.SKU,
                    VariantImage = x.Variant == null ? null : x.Variant.VariantImageUrl,
                    Quantity = x.TotalQuantity,
                    x.LastUpdateTime
                }).ToListAsync();
                var result = Helpers.LinqHelpers.ToPageList(items, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = items.Count,
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
    }
}
