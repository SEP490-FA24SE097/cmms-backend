using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/store-inventories")]
    public class StoreInventoryController : ControllerBase
    {
        private readonly IStoreInventoryService _storeInventoryService;

        public StoreInventoryController(IStoreInventoryService storeInventoryService)
        {
            _storeInventoryService = storeInventoryService;
        }




        [HttpGet("get-product-quantity")]
        public async Task<IActionResult> Get(GetProductQuantityDTO getProductQuantity)
        {
            try
            {
                var item = await _storeInventoryService.Get(x =>
                    x.StoreId == getProductQuantity.StoreId && x.MaterialId == getProductQuantity.MaterialId &&
                    x.VariantId == getProductQuantity.VariantId).FirstOrDefaultAsync();
                if (item == null)
                {
                    return Ok(new { success = true, message = "Không có hàng trong kho của cửa hàng" });
                }
                if (item.TotalQuantity == 0)
                {
                    return Ok(new { success = true, message = "Hết hàng" });
                }
                return Ok(new { success = true, quantity = item.TotalQuantity });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


    }
}
