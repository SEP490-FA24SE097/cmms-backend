using CMMS.Core.Entities;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMMS.Core.Models;
using static CMMS.API.TimeConverter.TimeConverter;
using CMMS.API.TimeConverter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Packaging.Signing;
using CMMS.Core.Enums;
using System.Security.Cryptography.X509Certificates;

namespace CMMS.API.Controllers
{
    [AllowAnonymous]
    [Route("api/imports")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _importService;
        private readonly IWarehouseService _warehouseService;
        private readonly IImportDetailService _importDetailService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IStoreService _storeService;
        private readonly IGoodsNoteService _goodsNoteService;
        private readonly IGoodsNoteDetailService _goodsNoteDetailService;
        public ImportController(IGoodsNoteDetailService goodsNoteDetailService, IGoodsNoteService goodsNoteService, IStoreService storeService, IStoreInventoryService storeInventoryService, IImportService importService, IWarehouseService warehouseService, IImportDetailService importDetailService, IVariantService variantService, IMaterialVariantAttributeService materialVariantAttributeService, IMaterialService materialService)
        {
            _importService = importService;
            _warehouseService = warehouseService;
            _importDetailService = importDetailService;
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _materialService = materialService;
            _storeInventoryService = storeInventoryService;
            _storeService = storeService;
            _goodsNoteService = goodsNoteService;
            _goodsNoteDetailService = goodsNoteDetailService;
        }

        // GET: api/imports
        [HttpGet]
        public IActionResult GetAll([FromQuery] int? page, [FromQuery] int? itemPerPage, [FromQuery] bool? isDateDescending, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? supplierId, [FromQuery] string? status)
        {
            try
            {
                var list = _importService.Get(x => (supplierId == null || x.SupplierId == supplierId) && (from == null || x.TimeStamp >= from) && (to == null || x.TimeStamp <= to)).Include(x => x.ImportDetails).ThenInclude(x => x.Material).ThenInclude(x => x.Variants).Include(x => x.Supplier).Where(x => status == null || x.Status == status).Select(x => new
                {
                    id = x.Id,
                    importCode = "IMP-" + x.Id.ToString().ToUpper().Substring(0, 4),
                    timeStamp = x.TimeStamp,
                    supplierName = x.Supplier == null ? null : x.Supplier.Name,
                    storeId = x.StoreId,
                    storeName = x.Store == null ? null : x.Store.Name,
                    status = x.Status,
                    note = x.Note,
                    totalQuantity = x.Quantity,
                    totalProduct = x.ImportDetails.Count > 0 ? x.ImportDetails.Count : 0,
                    totalPice = x.TotalPrice,
                    totalDiscount = x.TotalDiscount,
                    totalDue = x.TotalDue,
                    importDetails = x.ImportDetails.Count <= 0 ? null : x.ImportDetails.Select(x => new
                    {
                        materialCode = x.Material.MaterialCode,
                        name = x.Material.Name,
                        materialId = x.MaterialId,
                        variantId = x.VariantId,
                        sku = x.Variant == null ? null : x.Variant.SKU,
                        quantity = x.Quantity,
                        unitPrice = x.UnitPrice,
                        unitDiscount = x.UnitDiscount,
                        unitImportPrice = x.UnitPrice - x.UnitDiscount,
                        priceAfterDiscount = x.PriceAfterDiscount,
                        note = x.Note
                    }).ToList()

                }).ToList();
                if (isDateDescending != null)
                {
                    if ((bool)isDateDescending)
                    {
                        list = list.OrderByDescending(x => x.timeStamp).ToList();
                    }
                    if (!(bool)isDateDescending)
                    {
                        list = list.OrderBy(x => x.timeStamp).ToList();
                    }
                }
                else
                {
                    list = list.OrderByDescending(x => x.timeStamp).ToList();
                }

                return Ok(new
                {
                    data = Helpers.LinqHelpers.ToPageList(list, page == null ? 0 : (int)page - 1, itemPerPage == null ? 12 : (int)itemPerPage),
                    pagination = new
                    {
                        total = list.Count,
                        perPage = itemPerPage == null ? 12 : itemPerPage,
                        currentPage = page == null ? 1 : page
                    }


                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // GET: api/imports/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var import = _importService.Get(x=>x.Id==id).Include(x => x.ImportDetails).ThenInclude(x => x.Material).ThenInclude(x => x.Variants).Include(x => x.Supplier).Include(x => x.Store).Where(x => x.Id == id).Select(x => new
            {
                x.Id,
                importCode = "IMP-" + x.Id.ToString().ToUpper().Substring(0, 4),
                x.TimeStamp,
                supplierName = x.Supplier == null ? null : x.Supplier.Name,
                supplierId = x.SupplierId,
                storeId = x.StoreId,
                storeName = x.Store == null ? null : x.Store.Name,
                x.Status,
                x.Note,
                totalQuantity = x.Quantity,
                totalProduct = x.ImportDetails.Count,
                x.TotalPrice,
                x.TotalDiscount,
                x.TotalDue,
                importDetails = x.ImportDetails.Select(x => new
                {
                    x.Material.MaterialCode,
                    x.Material.Name,
                    x.MaterialId,
                    x.VariantId,
                    sku = x.Variant == null ? null : x.Variant.SKU,
                    x.Quantity,
                    x.UnitPrice,
                    x.UnitDiscount,
                    unitImportPrice = x.UnitPrice - x.UnitDiscount,
                    x.PriceAfterDiscount,
                    x.Note
                }).ToList()

            }).FirstOrDefault();
            if (import == null)
            {
                return NotFound();
            }
            return Ok(new { data = import });
        }

        // POST: api/imports
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ImportCM import)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (import.ImportDetails == null)
                {
                    return BadRequest("Import detail must not be null!");
                }
                var imp = new Import
                {
                    Id = Guid.NewGuid(),
                    Quantity = import.Quantity,
                    TotalPrice = import.TotalPrice,
                    TimeStamp = GetVietNamTime(),
                    SupplierId = import.SupplierId,
                    TotalDiscount = import.TotalDiscount,
                    TotalDue = import.TotalDue,
                    Note = import.Note,
                    StoreId = import.StoreId.IsNullOrEmpty() ? null : import.StoreId,
                    Status = import.IsCompleted ? "Đã nhập hàng" : "Phiếu tạm"
                };
                await _importService.AddAsync(imp);
                await _importService.SaveChangeAsync();
                await _importDetailService.AddRange(import.ImportDetails.Select(x => new ImportDetail()
                {
                    Id = Guid.NewGuid(),
                    ImportId = imp.Id,
                    VariantId = x.VariantId,
                    MaterialId = x.MaterialId,
                    PriceAfterDiscount = x.PriceAfterDiscount,
                    UnitDiscount = x.UnitDiscount,
                    UnitPrice = x.UnitPrice,
                    Quantity = x.Quantity,
                    Note = x.Note
                }));
                await _importDetailService.SaveChangeAsync();
                if (!import.StoreId.IsNullOrEmpty())
                {
                    if (import.IsCompleted)
                    {
                        var list = _importService.Get(x => x.Id == imp.Id).Include(x => x.ImportDetails)
                            .Select(x => x.ImportDetails).FirstOrDefault();
                        foreach (var item in list)
                        {
                            if (item.VariantId == null)
                            {
                                var storeInventory = _storeInventoryService
                                    .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId)
                                    .FirstOrDefault();
                                if (storeInventory != null)
                                {
                                    storeInventory.TotalQuantity += item.Quantity;
                                    storeInventory.LastUpdateTime = GetVietNamTime();
                                }
                                else
                                {
                                    await _storeInventoryService.AddAsync(new StoreInventory()
                                    {
                                        Id = Guid.NewGuid(),
                                        StoreId = import.StoreId,
                                        MaterialId = item.MaterialId,
                                        VariantId = item.VariantId,
                                        TotalQuantity = item.Quantity,
                                        MinStock = 10,
                                        MaxStock = 1000,
                                        InOrderQuantity = 0,
                                        ImportQuantity = 0,
                                        LastUpdateTime = GetVietNamTime()
                                    });
                                }

                                await _storeInventoryService.SaveChangeAsync();

                            }
                            else
                            {
                                var variant = _variantService.Get(x => x.Id == item.VariantId)
                                    .Include(x => x.ConversionUnit).FirstOrDefault();
                                if (variant != null)
                                {
                                    if (variant.ConversionUnitId == null)
                                    {
                                        var storeInventory = _storeInventoryService
                                            .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id)
                                            .FirstOrDefault();
                                        if (storeInventory != null)
                                        {
                                            storeInventory.TotalQuantity += item.Quantity;
                                            storeInventory.LastUpdateTime = GetVietNamTime();
                                        }
                                        else
                                        {
                                            await _storeInventoryService.AddAsync(new StoreInventory()
                                            {
                                                Id = Guid.NewGuid(),
                                                StoreId = import.StoreId,
                                                MaterialId = item.MaterialId,
                                                VariantId = item.VariantId,
                                                TotalQuantity = item.Quantity,
                                                MinStock = 10,
                                                MaxStock = 1000,
                                                InOrderQuantity = 0,
                                                ImportQuantity = 0,
                                                LastUpdateTime = GetVietNamTime()
                                            });
                                        }

                                        await _storeInventoryService.SaveChangeAsync();
                                    }
                                    else
                                    {
                                        var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                            .FirstOrDefault();
                                        if (rootVariant != null)
                                        {
                                            var storeInventory = _storeInventoryService
                                                .Get(x => x.MaterialId == rootVariant.MaterialId &&
                                                          x.VariantId == rootVariant.Id).FirstOrDefault();
                                            if (storeInventory != null)
                                            {
                                                storeInventory.TotalQuantity +=
                                                    item.Quantity * variant.ConversionUnit.ConversionRate;
                                                storeInventory.LastUpdateTime = GetVietNamTime();
                                            }
                                            else
                                            {
                                                await _storeInventoryService.AddAsync(new StoreInventory()
                                                {
                                                    Id = Guid.NewGuid(),
                                                    StoreId = import.StoreId,
                                                    MaterialId = item.MaterialId,
                                                    VariantId = item.VariantId,
                                                    TotalQuantity = item.Quantity,
                                                    MinStock = 10,
                                                    MaxStock = 1000,
                                                    InOrderQuantity = 0,
                                                    ImportQuantity = 0,
                                                    LastUpdateTime = GetVietNamTime()
                                                });
                                            }

                                            await _storeInventoryService.SaveChangeAsync();
                                        }
                                    }
                                }

                            }
                        }
                        var toStoreName = await _storeService.Get(x => x.Id == import.StoreId).Select(x => x.Name)
                            .FirstOrDefaultAsync();
                        var goodsNote = new GoodsNote()
                        {
                            Id = Guid.NewGuid(),
                            StoreId = import.StoreId,
                            ReasonDescription = $"Nhập hàng từ nhà cung cấp tới kho {toStoreName}",
                            TotalQuantity = import.Quantity,
                            Type = (int)GoodsNoteType.Receive,
                            TimeStamp = GetVietNamTime(),
                        };
                        await _goodsNoteService.AddAsync(goodsNote);
                        await _goodsNoteDetailService.AddRangeAsync(list.Select(x=>new GoodsNoteDetail()
                        {
                            Id = Guid.NewGuid(),
                            GoodsNoteId = goodsNote.Id,
                            MaterialId =x.MaterialId,
                            VariantId = x.VariantId,
                            Quantity = x.Quantity,
                        }));
                        await _goodsNoteDetailService.SaveChangeAsync();
                        if (await _storeInventoryService.SaveChangeAsync())
                        {
                            var store = _storeService.Get(x => x.Id == import.StoreId).Select(x => new
                            {
                                x.Id,
                                x.Name
                            }).FirstOrDefault();
                            return Ok(new
                            {
                                data = store
                            });
                        }

                    }
                }
                else
                {
                    if (import.IsCompleted)
                    {
                        var list = _importService.Get(x => x.Id == imp.Id).Include(x => x.ImportDetails)
                            .Select(x => x.ImportDetails).FirstOrDefault();
                        foreach (var item in list)
                        {
                            if (item.VariantId == null)
                            {
                                var warehouse = _warehouseService
                                    .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId)
                                    .FirstOrDefault();
                                if (warehouse != null)
                                {
                                    warehouse.TotalQuantity += item.Quantity;
                                    warehouse.LastUpdateTime = GetVietNamTime();
                                }
                                else
                                {
                                    await _warehouseService.AddAsync(new Warehouse
                                    {
                                        Id = Guid.NewGuid(),
                                        MaterialId = item.MaterialId,
                                        VariantId = item.VariantId,
                                        TotalQuantity = item.Quantity,
                                        LastUpdateTime = GetVietNamTime()
                                    });
                                }

                                await _warehouseService.SaveChangeAsync();

                            }
                            else
                            {
                                var variant = _variantService.Get(x => x.Id == item.VariantId)
                                    .Include(x => x.ConversionUnit).FirstOrDefault();
                                if (variant != null)
                                {
                                    if (variant.ConversionUnitId == null)
                                    {
                                        var warehouse = _warehouseService
                                            .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id)
                                            .FirstOrDefault();
                                        if (warehouse != null)
                                        {
                                            warehouse.TotalQuantity += item.Quantity;
                                            warehouse.LastUpdateTime = GetVietNamTime();
                                        }
                                        else
                                        {
                                            await _warehouseService.AddAsync(new Warehouse
                                            {
                                                Id = Guid.NewGuid(),
                                                MaterialId = variant.MaterialId,
                                                VariantId = variant.Id,
                                                TotalQuantity = item.Quantity,
                                                LastUpdateTime = GetVietNamTime()
                                            });
                                        }

                                        await _warehouseService.SaveChangeAsync();
                                    }
                                    else
                                    {
                                        var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                            .FirstOrDefault();
                                        if (rootVariant != null)
                                        {
                                            var warehouse = _warehouseService
                                                .Get(x => x.MaterialId == rootVariant.MaterialId &&
                                                          x.VariantId == rootVariant.Id).FirstOrDefault();
                                            if (warehouse != null)
                                            {
                                                warehouse.TotalQuantity +=
                                                    item.Quantity * variant.ConversionUnit.ConversionRate;
                                                warehouse.LastUpdateTime = GetVietNamTime();
                                            }
                                            else
                                            {
                                                await _warehouseService.AddAsync(new Warehouse
                                                {
                                                    Id = Guid.NewGuid(),
                                                    MaterialId = rootVariant.MaterialId,
                                                    VariantId = rootVariant.Id,
                                                    TotalQuantity = item.Quantity *
                                                                    variant.ConversionUnit.ConversionRate,
                                                    LastUpdateTime = GetVietNamTime()
                                                });
                                            }

                                            await _warehouseService.SaveChangeAsync();
                                        }
                                    }
                                }

                            }
                        }
                        var goodsNote = new GoodsNote()
                        {
                            Id = Guid.NewGuid(),
                            StoreId = null,
                            ReasonDescription = $"Nhập hàng từ nhà cung cấp tới kho tổng",
                            TotalQuantity = import.Quantity,
                            Type = (int)GoodsNoteType.Receive,
                            TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                        };
                        await _goodsNoteService.AddAsync(goodsNote);
                        await _goodsNoteDetailService.AddRangeAsync(list.Select(x => new GoodsNoteDetail()
                        {
                            Id = Guid.NewGuid(),
                            GoodsNoteId = goodsNote.Id,
                            MaterialId = x.MaterialId,
                            VariantId = x.VariantId,
                            Quantity = x.Quantity,
                        }));
                        await _goodsNoteDetailService.SaveChangeAsync();
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ImportUM import)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existImp = await _importService.Get(x => x.Id == import.ImportId).Include(x => x.ImportDetails)
                    .FirstOrDefaultAsync();
                if (existImp == null)
                {
                    return BadRequest("Phiếu nhập không tồn tại!!");
                }

                if (existImp != null && existImp.Status == "Đã nhập hàng")
                    return BadRequest("Không thể cập nhật phiếu đã nhập hàng!");
                if (existImp != null)
                {
                    existImp.Quantity = import.Quantity;
                    existImp.TotalPrice = import.TotalPrice;
                    existImp.TimeStamp = GetVietNamTime();
                    existImp.SupplierId = import.SupplierId;
                    existImp.TotalDiscount = import.TotalDiscount;
                    existImp.TotalDue = import.TotalDue;
                    existImp.Note = import.Note;
                    existImp.Status = import.Status.IsNullOrEmpty()?"Phiếu tạm":import.Status;
                    var updatedDetails = import.ImportDetails;
                    foreach (var updatedDetail in updatedDetails)
                    {
                        var existingDetail = existImp.ImportDetails.FirstOrDefault(x => x.Id == updatedDetail.Id);
                        if (existingDetail == null)
                        {
                            existImp.ImportDetails.Add(new ImportDetail()
                            {
                                Id = Guid.NewGuid(),
                                ImportId = existImp.Id,
                                VariantId = updatedDetail.VariantId,
                                MaterialId = updatedDetail.MaterialId,
                                PriceAfterDiscount = updatedDetail.PriceAfterDiscount,
                                UnitDiscount = updatedDetail.UnitDiscount,
                                UnitPrice = updatedDetail.UnitPrice,
                                Quantity = updatedDetail.Quantity,
                                Note = updatedDetail.Note
                            });
                        }
                        else
                        {
                            existingDetail.VariantId = updatedDetail.VariantId;
                            existingDetail.MaterialId = updatedDetail.MaterialId;
                            existingDetail.PriceAfterDiscount = updatedDetail.PriceAfterDiscount;
                            existingDetail.UnitDiscount = updatedDetail.UnitDiscount;
                            existingDetail.UnitPrice = updatedDetail.UnitPrice;
                            existingDetail.Quantity = updatedDetail.Quantity;
                            existingDetail.Note = updatedDetail.Note;
                        }
                    }

                    var detailIdsToKeep = updatedDetails.Select(x => x.Id).ToList();
                    var detailsToRemove = existImp.ImportDetails.Where(x => !detailIdsToKeep.Contains(x.Id)).ToList();

                    foreach (var detailToRemove in detailsToRemove)
                    {
                        existImp.ImportDetails.Remove(detailToRemove);
                    }

                    await _importService.SaveChangeAsync();
                    if (!existImp.StoreId.IsNullOrEmpty())
                    {
                        if (import.Status == "Đã nhập hàng")
                        {
                            var list = _importService.Get(x => x.Id == existImp.Id).Include(x => x.ImportDetails)
                                .Select(x => x.ImportDetails).FirstOrDefault();
                            foreach (var item in list)
                            {
                                if (item.VariantId == null)
                                {
                                    var storeInventory = _storeInventoryService
                                        .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId)
                                        .FirstOrDefault();
                                    if (storeInventory != null)
                                    {
                                        storeInventory.TotalQuantity += item.Quantity;
                                        storeInventory.LastUpdateTime = GetVietNamTime();
                                    }
                                    else
                                    {
                                        await _storeInventoryService.AddAsync(new StoreInventory()
                                        {
                                            Id = Guid.NewGuid(),
                                            StoreId = existImp.StoreId,
                                            MaterialId = item.MaterialId,
                                            VariantId = item.VariantId,
                                            TotalQuantity = item.Quantity,
                                            MinStock = 10,
                                            MaxStock = 1000,
                                            InOrderQuantity = 0,
                                            ImportQuantity = 0,
                                            LastUpdateTime = GetVietNamTime()
                                        });
                                    }

                                    await _storeInventoryService.SaveChangeAsync();

                                }
                                else
                                {
                                    var variant = _variantService.Get(x => x.Id == item.VariantId)
                                        .Include(x => x.ConversionUnit).FirstOrDefault();
                                    if (variant != null)
                                    {
                                        if (variant.ConversionUnitId == null)
                                        {
                                            var storeInventory = _storeInventoryService
                                                .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id)
                                                .FirstOrDefault();
                                            if (storeInventory != null)
                                            {
                                                storeInventory.TotalQuantity += item.Quantity;
                                                storeInventory.LastUpdateTime = GetVietNamTime();
                                            }
                                            else
                                            {
                                                await _storeInventoryService.AddAsync(new StoreInventory()
                                                {
                                                    Id = Guid.NewGuid(),
                                                    StoreId = existImp.StoreId,
                                                    MaterialId = item.MaterialId,
                                                    VariantId = item.VariantId,
                                                    TotalQuantity = item.Quantity,
                                                    MinStock = 10,
                                                    MaxStock = 1000,
                                                    InOrderQuantity = 0,
                                                    ImportQuantity = 0,
                                                    LastUpdateTime = GetVietNamTime()
                                                });
                                            }

                                            await _storeInventoryService.SaveChangeAsync();
                                        }
                                        else
                                        {
                                            var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                                .FirstOrDefault();
                                            if (rootVariant != null)
                                            {
                                                var storeInventory = _storeInventoryService
                                                    .Get(x => x.MaterialId == rootVariant.MaterialId &&
                                                              x.VariantId == rootVariant.Id).FirstOrDefault();
                                                if (storeInventory != null)
                                                {
                                                    storeInventory.TotalQuantity +=
                                                        item.Quantity * variant.ConversionUnit.ConversionRate;
                                                    storeInventory.LastUpdateTime = GetVietNamTime();
                                                }
                                                else
                                                {
                                                    await _storeInventoryService.AddAsync(new StoreInventory()
                                                    {
                                                        Id = Guid.NewGuid(),
                                                        StoreId = existImp.StoreId,
                                                        MaterialId = item.MaterialId,
                                                        VariantId = item.VariantId,
                                                        TotalQuantity = item.Quantity,
                                                        MinStock = 10,
                                                        MaxStock = 1000,
                                                        InOrderQuantity = 0,
                                                        ImportQuantity = 0,
                                                        LastUpdateTime = GetVietNamTime()
                                                    });
                                                }

                                                await _storeInventoryService.SaveChangeAsync();
                                            }
                                        }
                                    }

                                }
                            }
                            var toStoreName = await _storeService.Get(x => x.Id == existImp.StoreId).Select(x => x.Name)
                                .FirstOrDefaultAsync();
                            var goodsNote = new GoodsNote()
                            {
                                Id = Guid.NewGuid(),
                                StoreId = existImp.StoreId,
                                ReasonDescription = $"Nhập hàng từ nhà cung cấp tới kho {toStoreName}",
                                TotalQuantity = import.Quantity,
                                Type = (int)GoodsNoteType.Receive,
                                TimeStamp = GetVietNamTime(),
                            };
                            await _goodsNoteService.AddAsync(goodsNote);
                            await _goodsNoteDetailService.AddRangeAsync(list.Select(x => new GoodsNoteDetail()
                            {
                                Id = Guid.NewGuid(),
                                GoodsNoteId = goodsNote.Id,
                                MaterialId = x.MaterialId,
                                VariantId = x.VariantId,
                                Quantity = x.Quantity,
                            }));
                            await _goodsNoteDetailService.SaveChangeAsync();
                        }
                    }
                    else
                    {
                        if (import.Status == "Đã nhập hàng")
                        {
                            var list = _importService.Get(x => x.Id == existImp.Id).Include(x => x.ImportDetails)
                                .Select(x => x.ImportDetails).FirstOrDefault();
                            foreach (var item in list)
                            {
                                if (item.VariantId == null)
                                {
                                    var warehouse = _warehouseService
                                        .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId)
                                        .FirstOrDefault();
                                    if (warehouse != null)
                                    {
                                        warehouse.TotalQuantity += item.Quantity;
                                        warehouse.LastUpdateTime = GetVietNamTime();
                                    }
                                    else
                                    {
                                        await _warehouseService.AddAsync(new Warehouse
                                        {
                                            Id = Guid.NewGuid(),
                                            MaterialId = item.MaterialId,
                                            VariantId = item.VariantId,
                                            TotalQuantity = item.Quantity,
                                            LastUpdateTime = GetVietNamTime()
                                        });
                                    }

                                    await _warehouseService.SaveChangeAsync();

                                }
                                else
                                {
                                    var variant = _variantService.Get(x => x.Id == item.VariantId)
                                        .Include(x => x.ConversionUnit).FirstOrDefault();
                                    if (variant != null)
                                    {
                                        if (variant.ConversionUnitId == null)
                                        {
                                            var warehouse = _warehouseService
                                                .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id)
                                                .FirstOrDefault();
                                            if (warehouse != null)
                                            {
                                                warehouse.TotalQuantity += item.Quantity;
                                                warehouse.LastUpdateTime = GetVietNamTime();
                                            }
                                            else
                                            {
                                                await _warehouseService.AddAsync(new Warehouse
                                                {
                                                    Id = Guid.NewGuid(),
                                                    MaterialId = variant.MaterialId,
                                                    VariantId = variant.Id,
                                                    TotalQuantity = item.Quantity,
                                                    LastUpdateTime = GetVietNamTime()
                                                });
                                            }

                                            await _warehouseService.SaveChangeAsync();
                                        }
                                        else
                                        {
                                            var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                                .FirstOrDefault();
                                            if (rootVariant != null)
                                            {
                                                var warehouse = _warehouseService
                                                    .Get(x => x.MaterialId == rootVariant.MaterialId &&
                                                              x.VariantId == rootVariant.Id).FirstOrDefault();
                                                if (warehouse != null)
                                                {
                                                    warehouse.TotalQuantity +=
                                                        item.Quantity * variant.ConversionUnit.ConversionRate;
                                                    warehouse.LastUpdateTime = GetVietNamTime();
                                                }
                                                else
                                                {
                                                    await _warehouseService.AddAsync(new Warehouse
                                                    {
                                                        Id = Guid.NewGuid(),
                                                        MaterialId = rootVariant.MaterialId,
                                                        VariantId = rootVariant.Id,
                                                        TotalQuantity = item.Quantity *
                                                                        variant.ConversionUnit.ConversionRate,
                                                        LastUpdateTime = GetVietNamTime()
                                                    });
                                                }

                                                await _warehouseService.SaveChangeAsync();
                                            }
                                        }
                                    }

                                }
                            }
                            var goodsNote = new GoodsNote()
                            {
                                Id = Guid.NewGuid(),
                                StoreId = null,
                                ReasonDescription = $"Nhập hàng từ nhà cung cấp tới kho tổng",
                                TotalQuantity = import.Quantity,
                                Type = (int)GoodsNoteType.Receive,
                                TimeStamp = TimeConverter.TimeConverter.GetVietNamTime(),
                            };
                            await _goodsNoteService.AddAsync(goodsNote);
                            await _goodsNoteDetailService.AddRangeAsync(list.Select(x => new GoodsNoteDetail()
                            {
                                Id = Guid.NewGuid(),
                                GoodsNoteId = goodsNote.Id,
                                MaterialId = x.MaterialId,
                                VariantId = x.VariantId,
                                Quantity = x.Quantity,
                            }));
                            await _goodsNoteDetailService.SaveChangeAsync();
                        }
                    }
                    return Ok();
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }
        [HttpPost("cancel-import")]
        public async Task<IActionResult> CancelImport([FromQuery] Guid importId)
        {
            try
            {
                var import = await _importService.FindAsync(importId);
                if (import.Status != "Phiếu tạm")
                    return BadRequest("Không thể hủy phiếu đã nhập hàng");
                import.Status = "Đã hủy";
                await _importDetailService.SaveChangeAsync();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }

            return Ok();
        }

        [HttpPost("complete-import")]
        public async Task<IActionResult> CompleteImport([FromQuery] Guid importId)
        {
            try
            {
                var import = await _importService.FindAsync(importId);
                if (import.Status != "Phiếu tạm")
                    return BadRequest("Không thể hoàn thành phiếu nhập hàng");
                import.Status = "Đã nhập hàng";
                await _importDetailService.SaveChangeAsync();
                var list = _importService.Get(x => x.Id == importId).Include(x => x.ImportDetails)
                    .Select(x => x.ImportDetails).FirstOrDefault();
                foreach (var item in list)
                {
                    if (item.VariantId == null)
                    {
                        var warehouse = _warehouseService
                            .Get(x => x.MaterialId == item.MaterialId && x.VariantId == item.VariantId).FirstOrDefault();
                        if (warehouse != null)
                        {
                            warehouse.TotalQuantity += item.Quantity;
                            warehouse.LastUpdateTime = GetVietNamTime();
                        }
                        else
                        {
                            await _warehouseService.AddAsync(new Warehouse
                            {
                                Id = Guid.NewGuid(),
                                MaterialId = item.MaterialId,
                                VariantId = item.Id,
                                TotalQuantity = item.Quantity,
                                LastUpdateTime = GetVietNamTime()
                            });
                        }
                        await _warehouseService.SaveChangeAsync();

                    }
                    else
                    {
                        var variant = _variantService.Get(x => x.Id == item.VariantId).Include(x => x.ConversionUnit).FirstOrDefault();
                        if (variant != null)
                        {
                            if (variant.ConversionUnitId == null)
                            {
                                var warehouse = _warehouseService
                                    .Get(x => x.MaterialId == variant.MaterialId && x.VariantId == variant.Id).FirstOrDefault();
                                if (warehouse != null)
                                {
                                    warehouse.TotalQuantity += item.Quantity;
                                    warehouse.LastUpdateTime = GetVietNamTime();
                                }
                                else
                                {
                                    await _warehouseService.AddAsync(new Warehouse
                                    {
                                        Id = Guid.NewGuid(),
                                        MaterialId = variant.MaterialId,
                                        VariantId = variant.Id,
                                        TotalQuantity = item.Quantity,
                                        LastUpdateTime = GetVietNamTime()
                                    });
                                }
                                await _warehouseService.SaveChangeAsync();
                            }
                            else
                            {
                                var rootVariant = _variantService.Get(x => x.Id == variant.AttributeVariantId)
                                    .FirstOrDefault();
                                if (rootVariant != null)
                                {
                                    var warehouse = _warehouseService
                                        .Get(x => x.MaterialId == rootVariant.MaterialId && x.VariantId == rootVariant.Id).FirstOrDefault();
                                    if (warehouse != null)
                                    {
                                        warehouse.TotalQuantity += item.Quantity * variant.ConversionUnit.ConversionRate;
                                        warehouse.LastUpdateTime = GetVietNamTime();
                                    }
                                    else
                                    {
                                        await _warehouseService.AddAsync(new Warehouse
                                        {
                                            Id = Guid.NewGuid(),
                                            MaterialId = rootVariant.MaterialId,
                                            VariantId = rootVariant.Id,
                                            TotalQuantity = item.Quantity * variant.ConversionUnit.ConversionRate,
                                            LastUpdateTime = GetVietNamTime()
                                        });
                                    }
                                    await _warehouseService.SaveChangeAsync();
                                }
                            }
                        }

                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
