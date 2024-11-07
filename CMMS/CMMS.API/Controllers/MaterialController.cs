using System.Threading.Tasks.Dataflow;
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
    [AllowAnonymous]
    [ApiController]
    [Route("api/materials")]
    public class MaterialController : Controller
    {
        private readonly IMaterialService _materialService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        public MaterialController(IMaterialService materialService, IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _materialService = materialService;
            _materialVariantAttributeService = materialVariantAttributeService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll([FromQuery] int page, [FromQuery] int itemPerPage)
        {
            try
            {
                var list = _materialService.GetAll().
                    Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier)
                    .Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,
                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl,

                    }).ToList();
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = variants

                    });
                }
                var result = Helpers.LinqHelpers.ToPageList(newList, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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
        [HttpGet("general-filter")]
        [AllowAnonymous]
        public IActionResult GetFilter([FromQuery] int page, [FromQuery] int itemPerPage, MaterialFilterModel materialFilterModel)
        {
            try
            {
                var list = _materialService.GetAll().Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier)
                    .Where(x =>
                            (materialFilterModel.CategoryId == null || x.CategoryId == materialFilterModel.CategoryId)
                         && (materialFilterModel.BrandId == null || x.BrandId == materialFilterModel.BrandId)
                         && (materialFilterModel.SupplierId == null || x.SupplierId == materialFilterModel.SupplierId)
                         && (materialFilterModel.lowerPrice == null || x.SalePrice >= materialFilterModel.lowerPrice)
                         && (materialFilterModel.upperPrice == null || x.SalePrice <= materialFilterModel.upperPrice)
                    )
                    .Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,

                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl

                    }).ToList();
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = variants

                    });
                }
                var result = Helpers.LinqHelpers.ToPageList(newList, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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
        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult Search([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string? materialName)
        {
            try
            {
                var list = _materialService.GetAll().
                    Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.Name.Contains(materialName)).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,

                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl

                    }).ToList();
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = variants

                    });
                }
                var result = Helpers.LinqHelpers.ToPageList(newList, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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

        [HttpGet("get-materials-by-category")]
        [AllowAnonymous]
        public IActionResult GetByCategory([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string categoryId, [FromQuery] bool isDescendingPrice)
        {
            try
            {
                var list = _materialService.GetAll().Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.CategoryId == Guid.Parse(categoryId)).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,

                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl

                    }).ToList();
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = variants

                    });
                }
                List<MaterialVariantDTO> sortList = [];
                sortList = isDescendingPrice ? newList.OrderBy(x => x.Material.SalePrice).Reverse().ToList()
                    : newList.OrderBy(x => x.Material.SalePrice).ToList();
                var result = Helpers.LinqHelpers.ToPageList(sortList, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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
        [HttpGet("get-materials-by-brand")]
        [AllowAnonymous]
        public IActionResult GetByBrand([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string brandId, [FromQuery] bool isDescendingPrice)
        {
            try
            {
                var list = _materialService.GetAll().Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.BrandId == Guid.Parse(brandId)).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,

                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl

                    }).ToList();
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = variants

                    });
                }
                List<MaterialVariantDTO> sortList = [];
                sortList = isDescendingPrice ? newList.OrderBy(x => x.Material.SalePrice).Reverse().ToList()
                    : newList.OrderBy(x => x.Material.SalePrice).ToList();
                var result = Helpers.LinqHelpers.ToPageList(sortList, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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
        [HttpGet("get-materials-in-price-range")]
        [AllowAnonymous]
        public IActionResult GetByPrice([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] decimal lowerPrice, [FromQuery] decimal upperPrice)
        {
            try
            {
                var list = _materialService.GetAll().Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.SalePrice >= lowerPrice && x.SalePrice <= upperPrice).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,

                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl

                    }).ToList();
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = variants
                    });
                }
                List<MaterialVariantDTO> sortList = [];
                sortList = newList.OrderBy(x => x.Material.SalePrice).ToList();
                var result = Helpers.LinqHelpers.ToPageList(sortList, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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
        [HttpGet("get-materials-by-supplier")]
        [AllowAnonymous]
        public IActionResult GetBySupplier([FromQuery] int page, [FromQuery] int itemPerPage, [FromQuery] string supplierId)
        {
            try
            {
                var list = _materialService.GetAll().Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x => x.Supplier).Where(x => x.SupplierId == Guid.Parse(supplierId)).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,

                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl

                    }).ToList();
                List<MaterialVariantDTO> newList = [];
                foreach (var material in list)
                {
                    var variants = _materialVariantAttributeService.GetAll()
                        .Include(x => x.Variant)
                        .Include(x => x.Attribute).Where(x => x.Variant.MaterialId == material.Id).ToList()
                        .GroupBy(x => x.VariantId).Select(x => new VariantDTO()
                        {
                            VariantId = x.Key,
                            Sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                            Image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                            Price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                            Attributes = x.Select(x => new AttributeDTO()
                            {
                                Name = x.Attribute.Name,
                                Value = x.Value
                            }).ToList()
                        }).ToList();
                    newList.Add(new MaterialVariantDTO()
                    {
                        Material = material,
                        Variants = variants

                    });
                }
                var result = Helpers.LinqHelpers.ToPageList(newList, page - 1, itemPerPage);

                return Ok(new
                {
                    data = result,
                    pagination = new
                    {
                        total = list.Count(),
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
        [HttpGet("get-all-materials-with-name-unit")]
        [AllowAnonymous]
        public IActionResult GetAllNames()
        {
            try
            {
                var result = _materialService.GetAll().Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    Unit = x.Unit
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpGet("{id}")]
        public IActionResult GetMaterialById([FromRoute] string id)
        {
            try
            {
                var result = _materialService.Get(x => x.Id == Guid.Parse(id)).
                    Include(x => x.Brand).
                    Include(x => x.Category).
                    Include(x => x.Unit).
                    Include(x=>x.SubImages).
                    Include(x => x.Supplier).Select(x => new MaterialDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        BarCode = x.BarCode,
                        Brand = x.Brand.Name,
                        IsRewardEligible = x.IsRewardEligible,
                        Description = x.Description,

                        SalePrice = x.SalePrice,
                        Unit = x.Unit.Name,
                        Supplier = x.Supplier.Name,
                        Category = x.Category.Name,
                        MinStock = x.MinStock,
                        ImageUrl = x.ImageUrl,
                        SubImages = x.SubImages.Select(x => new SubImageDTO()
                        {
                            Id = x.Id,
                            SubImageUrl = x.SubImageUrl
                        }).ToList()
                    }).FirstOrDefault();
                var variants = _materialVariantAttributeService.GetAll()
                    .Include(x => x.Variant)
                    .Include(x => x.Attribute)
                    .Where(x => x.Variant.MaterialId == Guid.Parse(id)).ToList().GroupBy(x => x.VariantId);
                return Ok(new
                {
                    material = result,
                    variants = variants.Select(x => new
                    {
                        variantId = x.Key,
                        sku = x.Select(x => x.Variant.SKU).FirstOrDefault(),
                        image = x.Select(x => x.Variant.VariantImageUrl).FirstOrDefault(),
                        price = x.Select(x => x.Variant.Price).FirstOrDefault(),
                        attributes = x.Select(x => new
                        {
                            x.Attribute.Name,
                            x.Value
                        })
                    })
                });
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
                    BarCode = materialCm.BarCode,
                    Description = materialCm.Description,
                    ImageUrl = materialCm.ImageUrl,
                    SalePrice = materialCm.SalePrice,
                    MinStock = materialCm.MinStock,
                    BrandId = materialCm.BrandId,
                    SupplierId = materialCm.SupplierId,
                    UnitId = materialCm.UnitId,
                    CategoryId = materialCm.CategoryId,
                    IsRewardEligible = materialCm.IsRewardEligible
                };
                await _materialService.AddAsync(material);
                await _materialService.SaveChangeAsync();

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
                material.BarCode = materialUM.BarCode.IsNullOrEmpty() ? material.BarCode : materialUM.BarCode;
                material.Description = materialUM.Description.IsNullOrEmpty() ? material.Description : materialUM.Description;
                material.ImageUrl = materialUM.ImageUrl.IsNullOrEmpty() ? material.ImageUrl : materialUM.ImageUrl;
                material.SalePrice = materialUM.SalePrice == 0 ? material.SalePrice : materialUM.SalePrice;
                material.MinStock = materialUM.MinStock == 0 ? material.MinStock : materialUM.MinStock;
                material.SupplierId = materialUM.SupplierId.IsNullOrEmpty() ? material.SupplierId : Guid.Parse(materialUM.SupplierId);
                material.BrandId = materialUM.BrandId.IsNullOrEmpty() ? material.BrandId : Guid.Parse(materialUM.BrandId);
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
