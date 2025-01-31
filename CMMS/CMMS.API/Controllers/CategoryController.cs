﻿using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [AllowAnonymous]
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
                var result = _categoryService.Get(x => x.ParentCategoryId == null).Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    subCategories = x.Categories.Select(x => new
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).ToList()
                });
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-sub-categories")]
        [AllowAnonymous]
        public IActionResult GetSubCategoies([FromQuery] Guid parentCategoryId)
        {
            try
            {
                var result = _categoryService.Get(x => x.ParentCategoryId == parentCategoryId).Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name

                }).ToList();
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCateById([FromRoute] string id)
        {
            try
            {
                var result = await _categoryService.FindAsync(Guid.Parse(id));
                return Ok(new{data=result});
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HasPermission(Infrastructure.Enums.PermissionName.SeniorPermission)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<CategoryCM> categories)
        {
            try
            {
                await _categoryService.AddRange(categories.Select(x => new Category
                {
                    Id = Guid.NewGuid(),
                    Name = x.Name,
                    ParentCategoryId = x.ParentCategoryId
                }));
                await _categoryService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HasPermission(Infrastructure.Enums.PermissionName.SeniorPermission)]
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
        [HasPermission(Infrastructure.Enums.PermissionName.SeniorPermission)]
        [HttpDelete("delete-category")]
        public async Task<IActionResult> Delete([FromQuery] string categoryId)
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
