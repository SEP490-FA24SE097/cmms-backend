using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Attribute = CMMS.Core.Entities.Attribute;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/attributes")]
    public class AttributeController : ControllerBase
    {
        private readonly IAttributeService _attributeService;

        public AttributeController(IAttributeService attributeService)
        {
            _attributeService = attributeService;
        }

        // GET: api/attributes
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            try
            {
                var result = _attributeService.GetAll().Select(x => new
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

        // GET: api/attributes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            try
            {
                var attribute = await _attributeService.FindAsync(id);
                if (attribute == null)
                {
                    return NotFound(new { success = false, message = "Attribute not found" });
                }
                return Ok(attribute);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // POST: api/attributes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<string> attributes)
        {
            try
            {
                var newAttributes = attributes.Select(x => new Attribute
                {
                    Id = Guid.NewGuid(),
                    Name = x
                }).ToList();

                await _attributeService.AddRange(newAttributes);
                await _attributeService.SaveChangeAsync();

                return Ok(new { success = true, message = "Attributes created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // PUT: api/attributes
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AttributeUM attributeUM)
        {
            try
            {
                var existingAttribute = await _attributeService.FindAsync(attributeUM.Id);
                if (existingAttribute == null)
                {
                    return NotFound(new { success = false, message = "Attribute not found" });
                }

                // Check for name conflict
                if (_attributeService.Get(a => a.Name == attributeUM.Name && a.Id != attributeUM.Id).Any())
                {
                    return BadRequest(new { success = false, message = "Attribute name already exists" });
                }

                // Update the attribute
                existingAttribute.Name = attributeUM.Name;
                _attributeService.Update(existingAttribute);
                await _attributeService.SaveChangeAsync();

                return Ok(new { success = true, message = "Attribute updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // DELETE: api/attributes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                var attributeExists = await _attributeService.FindAsync(id);
                if (attributeExists == null)
                {
                    return NotFound(new { success = false, message = "Attribute not found" });
                }

                await _attributeService.Remove(id);
                await _attributeService.SaveChangeAsync();

                return Ok(new { success = true, message = "Attribute deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
