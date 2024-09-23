using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private IStoreService _storeService;

        public StoreController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet("GetAllStore")]
        public async Task<IActionResult> GetAll([FromQuery] DefaultSearch defaultSearch) {
            var result = _storeService.GetAllStore();
            var data = result.Sort(string.IsNullOrEmpty(defaultSearch.sortBy) ? "Name" : defaultSearch.sortBy
                      , defaultSearch.isAscending)
                      .ToPageList(defaultSearch.currentPage, defaultSearch.perPage).ToList();
            return Ok(new { total = result.ToList().Count, data, page = defaultSearch.currentPage });
        }

        [HttpPost("AddNewStore")]
        public async Task<IActionResult> AddNewStore(StoreCM store)
        {
            await _storeService.CreateNewStore(store);
            return Ok();
        }
    }
}
