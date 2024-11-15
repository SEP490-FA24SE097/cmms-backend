using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/variants")]
    public class VariantController : ControllerBase
    {
        private readonly IVariantService _variantService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IMaterialService _materialService;
        public VariantController(IVariantService variantService, IMaterialVariantAttributeService materialVariantAttributeService, IMaterialService materialService)
        {
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _materialService = materialService;
        }

        // GET: api/variants
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            try
            {
                var result = _variantService.GetAll().Select(v => new
                {
                    Id = v.Id,
                    MaterialId = v.MaterialId,
                    SKU = v.SKU,
                    Price = v.Price,
                    VariantImageUrl = v.VariantImageUrl
                });
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // GET: api/variants/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            try
            {
                var variant = await _variantService.FindAsync(id);
                if (variant == null)
                {
                    return NotFound(new { success = false, message = "Variant not found" });
                }

                var attributes = _materialVariantAttributeService.Get(x => x.VariantId == id).Include(x => x.Attribute).Select(x => new
                {
                    attributeName = x.Attribute.Name,
                    value = x.Value
                }).ToList();
                return Ok(new
                {
                    Id = variant.Id,
                    MaterialId = variant.MaterialId,
                    SKU = variant.SKU,
                    Price = variant.Price,
                    variant.CostPrice,
                    variant.ConversionUnitId,
                  //  ConversionUnitName = variant.ConversionUnitId == null ? null : variant.ConversionUnit.Name,
                    VariantImageUrl = variant.VariantImageUrl,
                    attributes = attributes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-by-material-id")]
        public async Task<IActionResult> GetByMaterialId([FromRoute] Guid materialId)
        {
            try
            {
                var variants = _variantService.Get(x => x.MaterialId == materialId).Include(x => x.ConversionUnit).ToList();
                var variantIds = variants.Select(x => x.Id).ToList();
                var attributes = _materialVariantAttributeService.Get(x => variantIds.Contains(x.VariantId))
                    .Include(x => x.Attribute).Include(x => x.Variant).ThenInclude(x => x.ConversionUnit)
               .GroupBy(x => x.VariantId).ToList();

                if (attributes.Count <= 0)
                {
                    var unitVariants = variants.Select(x => new
                    {
                        VariantId = x.Id,
                        ConversionUnitId = x.ConversionUnitId,
                       // ConversionUnitName = x.ConversionUnit.Name,
                        Sku = x.SKU,
                        Image = x.VariantImageUrl,
                        Price = x.Price,
                        CostPrice = x.CostPrice
                    }).ToList();
                    return Ok(new { data = unitVariants });
                }
                var list = attributes.Select(x => new
                {
                    Id = x.Key,
                    MaterialId = materialId,
                    SKU = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                    Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                    CostPrice = x.Select(x => x.Variant.CostPrice).FirstOrDefault(),
                    ConversionUnitId = x.Select(x => x.Variant.ConversionUnitId).FirstOrDefault(),
                    //ConversionUnitName = x.Select(x => x.Variant.ConversionUnit).Any() ? null : x.Select(x => x.Variant.ConversionUnit.Name).FirstOrDefault(),
                    VariantImageUrl = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                    attributes = x.Select(x => new { x.Attribute.Name, x.Value })
                }).ToList();

                return Ok(new { data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        // POST: api/variants
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VariantCM variant)
        {
            try
            {
                var material = await _materialService.FindAsync(variant.MaterialId);
                var unitVariant =
                    _variantService.Get(x => x.MaterialId == variant.MaterialId && x.MaterialVariantAttributes.Count <= 0)
                        .Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                if (unitVariant != null)
                {
                    return BadRequest();
                }
                var dic = variant.Attributes.ToDictionary(x => x.Id, x => x.Value);
                var newVariant = new Variant
                {
                    Id = Guid.NewGuid(),
                    MaterialId = variant.MaterialId,
                    SKU = variant.SKU == null ? material.Name + "-" + string.Join("-", dic.Values) : variant.SKU,
                    Price = variant.Price,
                    CostPrice = variant.CostPrice,
                    VariantImageUrl = variant.VariantImageUrl
                };
                await _variantService.AddAsync(newVariant);
                await _variantService.SaveChangeAsync();
                await _materialVariantAttributeService.AddRange(dic.Select(x =>
                    new MaterialVariantAttribute
                    {
                        Id = new Guid(),
                        VariantId = newVariant.Id,
                        AttributeId = x.Key,
                        Value = x.Value
                    }));
                await _materialVariantAttributeService.SaveChangeAsync();
                return Ok(new { success = true, message = "Variants created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // PUT: api/variants
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] VariantUM variantUM)
        {
            try
            {
                var existingVariant = await _variantService.FindAsync(variantUM.Id);
                if (existingVariant == null)
                {
                    return NotFound(new { success = false, message = "Variant not found" });
                }

                // Check if SKU is unique
                if (_variantService.Get(v => v.SKU == variantUM.SKU && v.Id != variantUM.Id).Any())
                {
                    return BadRequest(new { success = false, message = "SKU is already in use" });
                }

                // Update the variant

                existingVariant.SKU = variantUM.SKU;
                existingVariant.Price = variantUM.Price;
                existingVariant.CostPrice = variantUM.CostPrice;
                existingVariant.VariantImageUrl = variantUM.VariantImageUrl;

                _variantService.Update(existingVariant);
                await _variantService.SaveChangeAsync();

                return Ok(new { success = true, message = "Variant updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // DELETE: api/variants/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                var variant = await _variantService.FindAsync(id);
                if (variant == null)
                {
                    return NotFound(new { success = false, message = "Variant not found" });
                }

                await _variantService.Remove(id);
                await _variantService.SaveChangeAsync();

                return Ok(new { success = true, message = "Variant deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
