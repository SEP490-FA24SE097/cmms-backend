using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/brands")]
    public class BrandController : Controller
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        // Get all brands
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            try
            {
                var result = _brandService.GetAll().Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name
                });
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Get a brand by its ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrandById([FromRoute] string id)
        {
            try
            {
                var result = await _brandService.FindAsync(Guid.Parse(id));
                return result != null ? Ok(result) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Create new brands
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<string> brands)
        {
            try
            {
                await _brandService.AddRange(brands.Select(x => new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = x
                }));
                await _brandService.SaveChangeAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Update an existing brand
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] BrandUM brandUM)
        {
            try
            {
                // Check if another brand with the same name exists
                if (_brandService.Get(b => b.Name.Contains(brandUM.Name)).Any())
                {
                    return BadRequest(new { success = false, message = "Brand name is already existed!" });
                }

                var brand = await _brandService.FindAsync(brandUM.Id);
                if (brand == null)
                {
                    return NotFound();
                }

                brand.Name = brandUM.Name;
                _brandService.Update(brand);
                await _brandService.SaveChangeAsync();
                return Ok(brand);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Delete a brand by its ID
        [HttpDelete("delete-brand")]
        public async Task<IActionResult> Delete([FromQuery] string brandId)
        {
            try
            {
                var result = await _brandService.Remove(Guid.Parse(brandId));
                if (!result)
                {
                    return NotFound();
                }

                await _brandService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
