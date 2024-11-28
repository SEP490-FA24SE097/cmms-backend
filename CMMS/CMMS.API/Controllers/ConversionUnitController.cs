using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using DinkToPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/conversion-units")]
    [ApiController]
    public class ConversionUnitController : ControllerBase
    {
        private readonly IConversionUnitService _conversionUnitService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;

        public ConversionUnitController(IConversionUnitService conversionUnitService, IVariantService variantService, IMaterialService materialService, IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _conversionUnitService = conversionUnitService;
            _variantService = variantService;
            _materialService = materialService;
            _materialVariantAttributeService = materialVariantAttributeService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] Guid materialId, [FromBody] List<ConversionUnitCM> conversionUnitName)
        {
            try
            {
                var list = conversionUnitName.Select(x => new ConversionUnit()
                {
                    Id = new Guid(),
                    //  Name = x.Name,
                    ConversionRate = x.ConversionRate,
                    Price = x.Price,
                    MaterialId = materialId
                }).ToList();
                await _conversionUnitService.AddRange(list);
                await _conversionUnitService.SaveChangeAsync();

                var variants = _variantService.Get(x => x.MaterialId == materialId).ToList();
                if (!variants.Any())
                {
                    var material = _materialService.Get(x => x.Id == materialId).FirstOrDefault();
                    if (material == null)
                    {
                        return BadRequest();
                    }

                    var newVariant = new Variant()
                    {
                        Id = new Guid(),
                        VariantImageUrl = material.ImageUrl,
                        Price = material.SalePrice,
                        CostPrice = material.CostPrice,
                        ConversionUnitId = null,
                        SKU = material.Name + " (" + material.Unit.Name + ")",
                        MaterialId = materialId
                    };
                    await _variantService.AddAsync(newVariant);

                    //await _variantService.AddRange(list.Select(x => new Variant()
                    //{
                    //    Id = new Guid(),
                    //    VariantImageUrl = material.ImageUrl,
                    //    Price = x.Price == 0 ? material.SalePrice * x.ConversionRate : x.Price,
                    //    CostPrice = material.CostPrice * x.ConversionRate,
                    //    ConversionUnitId = x.Id,
                    //    // SKU = material.Name + " (" + x.Name + ")",
                    //    SKU = material.Name + " (" + newVariant.Unit.Name + ")",
                    //    AttributeVariantId = newVariant.Id,
                    //    MaterialId = materialId
                    //}));

                    foreach (var item in list)
                    {
                        var unitName = _conversionUnitService.Get(x => x.Id == item.Id).Include(x => x.Unit).FirstOrDefault();
                        await _variantService.AddAsync(new Variant()
                        {
                            Id = new Guid(),
                            VariantImageUrl = material.ImageUrl,
                            Price = item.Price == 0 ? material.SalePrice * item.ConversionRate : item.Price,
                            CostPrice = material.CostPrice * item.ConversionRate,
                            ConversionUnitId = item.Id,
                            AttributeVariantId = newVariant.Id,
                            SKU = material.Name + " (" + unitName.Unit.Name + ")",
                            MaterialId = material.Id
                        });
                    }

                    await _variantService.SaveChangeAsync();
                }
                else
                {
                    var attributeVariants = _variantService.Get(x => x.MaterialId == materialId && x.MaterialVariantAttributes.Count > 0 && x.ConversionUnitId == null).ToList();
                    var unitVariants = _variantService
                        .Get(x => x.MaterialId == materialId && x.MaterialVariantAttributes.Count <= 0).AsQueryable().Include(x => x.MaterialVariantAttributes).ToList();
                    if (unitVariants.Count > 0)
                    {
                        var material = await _materialService.FindAsync(materialId);
                        //await _variantService.AddRange(list.Select(x => new Variant()
                        //{
                        //    Id = new Guid(),
                        //    VariantImageUrl = material.ImageUrl,
                        //    Price = x.Price == 0 ? material.SalePrice * x.ConversionRate : x.Price,
                        //    CostPrice = material.CostPrice * x.ConversionRate,
                        //    ConversionUnitId = x.Id,
                        //  //  SKU = material.Name + " (" + x.Name + ")",
                        //    MaterialId = materialId
                        //}));
                        foreach (var item in list)
                        {
                            var unitName = _conversionUnitService.Get(x => x.Id == item.Id).Include(x => x.Unit).FirstOrDefault();
                            var rootUnitVariant = await _variantService
                                .Get(x => x.MaterialId == materialId && x.ConversionUnitId == null)
                                .FirstOrDefaultAsync();
                            await _variantService.AddAsync(new Variant()
                            {
                                Id = new Guid(),
                                VariantImageUrl = material.ImageUrl,
                                Price = item.Price == 0 ? material.SalePrice * item.ConversionRate : item.Price,
                                CostPrice = material.CostPrice * item.ConversionRate,
                                ConversionUnitId = item.Id,
                                AttributeVariantId = rootUnitVariant.Id,
                                SKU = material.Name + " (" + unitName.Unit.Name + ")",
                                MaterialId = material.Id
                            });
                        }
                        await _variantService.SaveChangeAsync();
                    }

                    if (attributeVariants.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            var unitName = _conversionUnitService.Get(x => x.Id == item.Id).Include(x => x.Unit).FirstOrDefault();
                            await _variantService.AddRange(attributeVariants.Select(x => new Variant()
                            {
                                Id = new Guid(),
                                VariantImageUrl = x.VariantImageUrl,
                                Price = x.Price * item.ConversionRate,
                                CostPrice = x.CostPrice * item.ConversionRate,
                                ConversionUnitId = item.Id,
                                //  SKU = x.SKU + " (" + unit.Name + ")",
                                SKU = x.SKU + " (" + unitName.Unit.Name + ")",
                                AttributeVariantId = x.Id,
                                MaterialId = materialId
                            }));
                        }
                        await _variantService.SaveChangeAsync();
                    }

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
