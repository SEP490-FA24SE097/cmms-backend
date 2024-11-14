using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [Route("api/conversion-units")]
    [ApiController]
    public class ConversionUnitController : ControllerBase
    {
        private readonly IConversionUnitService _conversionUnitService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;

        public ConversionUnitController(IConversionUnitService conversionUnitService, IVariantService variantService, IMaterialService materialService)
        {
            _conversionUnitService = conversionUnitService;
            _variantService = variantService;
            _materialService = materialService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] Guid materialId, [FromBody] List<ConversionUnitCM> conversionUnitName)
        {
            try
            {
                var list = conversionUnitName.Select(x => new ConversionUnit()
                {
                    Id = new Guid(),
                    Name = x.Name,
                    ConversionRate = x.ConversionRate,
                    Price = x.Price,
                    MaterialId = materialId
                }).ToList();
                await _conversionUnitService.AddRange(list);
                await _conversionUnitService.SaveChangeAsync();

                var variants = _variantService.Get(x => x.MaterialId == materialId).ToList();
                if (!variants.Any())
                {
                    var material = await _materialService.FindAsync(materialId);
                    await _variantService.AddRange(list.Select(x => new Variant()
                    {
                        Id = new Guid(),
                        VariantImageUrl = material.ImageUrl,
                        Price = material.SalePrice * x.ConversionRate,
                        CostPrice = material.CostPrice * x.ConversionRate,
                        ConversionUnitId = x.Id,
                        SKU = material.Name + " (" + x.Name + ")",
                        MaterialId = materialId
                    }));
                }
                else
                {
                    foreach (var unit in list)
                    {
                        await _variantService.AddRange(variants.Select(x => new Variant()
                        {
                            Id = new Guid(),
                            VariantImageUrl = x.VariantImageUrl,
                            Price = x.Price * unit.ConversionRate,
                            CostPrice = x.CostPrice * unit.ConversionRate,
                            ConversionUnitId = unit.Id,
                            SKU = x.SKU + " (" + unit.Name + ")",
                            MaterialId = materialId
                        }));
                    }
                    await _variantService.SaveChangeAsync();
                }
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }

        }
    }
}
