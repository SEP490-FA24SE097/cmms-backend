using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/materials")]
    public class MaterialController : Controller
    {
        private readonly IMaterialService _materialService;
        private readonly IImageService _imageService;
        public MaterialController(IMaterialService materialService, IImageService imageService)
        {
            _materialService = materialService;
            _imageService = imageService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll([FromQuery] int page, [FromQuery] int itemPerPage)
        {
            try
            {
                var list = _materialService.GetAll().
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SoldQuantity = x.SoldQuantity,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,
                        CostPrice = x.CostPrice,
                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        Images = x.Images

                    });
                var result = Helpers.LinqHelpers.ToPageList(list, page, itemPerPage);

                return Ok(new
                {
                    data = result,
                    total = list.Count(),
                    perPage = itemPerPage,
                    currentPage = page

                });



            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult Search([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string? materialName)
        {
            try
            {
                var list = _materialService.GetAll().
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.Name.Contains(materialName)).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SoldQuantity = x.SoldQuantity,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,
                        CostPrice = x.CostPrice,
                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        Images = x.Images

                    });
                var result = Helpers.LinqHelpers.ToPageList(list, page, itemPerPage);

                return Ok(new
                {
                    data = result,
                    total = list.Count(),
                    perPage = itemPerPage,
                    currentPage = page

                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-materials-by-category")]
        [AllowAnonymous]
        public IActionResult GetByCategory([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string categoryId)
        {
            try
            {
                var list = _materialService.GetAll().
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.CategoryId == Guid.Parse(categoryId)).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SoldQuantity = x.SoldQuantity,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,
                        CostPrice = x.CostPrice,
                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        Images = x.Images

                    });
                var result = Helpers.LinqHelpers.ToPageList(list, page, itemPerPage);

                return Ok(new
                {
                    data = result,
                    total = list.Count(),
                    perPage = itemPerPage,
                    currentPage = page

                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-materials-by-supplier")]
        [AllowAnonymous]
        public IActionResult GetBySupplier([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string supplierId)
        {
            try
            {
                var list = _materialService.GetAll().
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.CategoryId == Guid.Parse(supplierId)).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SoldQuantity = x.SoldQuantity,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,
                        CostPrice = x.CostPrice,
                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        Images = x.Images

                    });
                var result = Helpers.LinqHelpers.ToPageList(list, page, itemPerPage);

                return Ok(new
                {
                    data = result,
                    total = list.Count(),
                    perPage = itemPerPage,
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("get-all-material-names")]
        [AllowAnonymous]
        public IActionResult GetAllNames()
        {
            try
            {
                var result = _materialService.GetAll().Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("id")]
        public IActionResult GetMaterialById(string id)
        {
            try
            {
                var result = _materialService.Get(x => x.Id == Guid.Parse(id)).Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SoldQuantity = x.SoldQuantity,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,
                        CostPrice = x.CostPrice,
                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        Images = x.Images
                    }).FirstOrDefault();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MaterialCM materialCm)
        {
            try
            {
                var material = new Material
                {
                    Id = new Guid(),
                    Name = materialCm.Name,
                    Description = materialCm.Description,
                    CostPrice = materialCm.CostPrice,
                    SalePrice = materialCm.SalePrice,
                    MinStock = materialCm.MinStock,
                    SoldQuantity = 0,
                    SupplierId = materialCm.SupplierId,
                    UnitId = materialCm.UnitId,
                    CategoryId = materialCm.CategoryId,
                    IsRewardEligible = materialCm.IsRewardEligible
                };
                await _materialService.AddAsync(material);
                await _materialService.SaveChangeAsync();
                await _imageService.AddRange(materialCm.Images.Select(x => new Image
                {
                    Id = new Guid(),
                    ImageUrl = x.ImageUrl,
                    MaterialId = material.Id,
                    IsMainImage = x.IsMainImage
                }));
                await _imageService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromQuery] string materialId, [FromBody] MaterialUM materialUM)
        {
            try
            {
                var material = await _materialService.FindAsync(Guid.Parse(materialId));
                material.Name = materialUM.Name.IsNullOrEmpty() ? material.Name : materialUM.Name;
                material.Description = materialUM.Description.IsNullOrEmpty() ? material.Description : materialUM.Description;
                material.CostPrice = materialUM.CostPrice == 0 ? material.CostPrice : materialUM.CostPrice;
                material.SalePrice = materialUM.SalePrice == 0 ? material.SalePrice : materialUM.SalePrice;
                material.MinStock = materialUM.MinStock == 0 ? material.MinStock : materialUM.MinStock;
                material.SupplierId = materialUM.SupplierId.IsNullOrEmpty() ? material.Id : Guid.Parse(materialUM.SupplierId);
                material.CategoryId = materialUM.CategoryId.IsNullOrEmpty() ? material.CategoryId : Guid.Parse(materialUM.CategoryId);
                material.UnitId = materialUM.UnitId.IsNullOrEmpty() ? material.UnitId : Guid.Parse(materialUM.UnitId);
                material.IsRewardEligible = materialUM.IsRewardEligible;
                await _materialService.SaveChangeAsync();
                return Ok(material);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpDelete("delete-material")]
        public async Task<IActionResult> Delete([FromQuery] string materialId)
        {
            try
            {
                await _materialService.Remove(Guid.Parse(materialId));
                await _materialService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
