﻿using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            try
            {
                var result = _categoryService.GetAll().Select(x=>new
                {
                    Id=x.Id,
                    Name=x.Name
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("id")]
        public async Task<IActionResult> GetCateById(string id)
        {
            try
            {
                var result = await _categoryService.FindAsync(Guid.Parse(id));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<string> categories)
        {
            try
            {
                await _categoryService.AddRange(categories.Select(x=>new Category
                {
                    Id = new Guid(),
                    Name = x
                }));
                await _categoryService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CategoryUM categoryUM)
        {
            try
            {
                if (_categoryService.Get(c => c.Name.Contains(categoryUM.Name)).Any())
                {
                    return BadRequest(new { success = false, message = "Category name is already existed !" });
                }
                var result = await _categoryService.FindAsync(categoryUM.Id);
                result.Name = categoryUM.Name;
                _categoryService.Update(result);
                await _categoryService.SaveChangeAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpDelete("delete-category")]
        public async Task<IActionResult> Update([FromQuery] string categoryId)
        {
            try
            {
                await _categoryService.Remove(Guid.Parse(categoryId));
                await _categoryService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}