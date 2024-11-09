using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Models;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/store")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private IStoreService _storeService;

        public StoreController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet("stores")]
        public async Task<IActionResult> GetAll([FromQuery] DefaultSearch defaultSearch, StoreType storeType) {
            var result = _storeService.GetAllStore(storeType);
            var data = result.Sort(string.IsNullOrEmpty(defaultSearch.sortBy) ? "Name" : defaultSearch.sortBy
                      , defaultSearch.isAscending)
                      .ToPageList(defaultSearch.currentPage, defaultSearch.perPage).ToList();
            return Ok(new { total = result.ToList().Count, data, page = defaultSearch.currentPage });
        }

        [HttpGet("{storeId}")]
        public async Task<IActionResult> GetStoreById(string storeId)
        {
            var result = await _storeService.GetStoreById(storeId);
            if(result == null) return NotFound("Store not found");
            return Ok(result);
        }


        [HttpPost("add-new")]
        public async Task<IActionResult> AddNewStore(StoreCM store)
        {
            var result = await _storeService.CreateNewStore(store);
            return Ok(result);
        }


        [HttpPost("assign-user-to-managed-store")]
        public async Task<IActionResult> AssignUserToManagedStore(string userId, string storeId)
        {
            var result = await _storeService.AssignUserToManageStore(userId, storeId);
            return Ok(result);
        }


        [HttpPost("remove-user-managed-store")]
        public async Task<IActionResult> RemoveUserManagedStore(string userId, string storeId)
        {
            var result = await _storeService.RemoveUserManagedStore(userId, storeId);
            return Ok(result);
        }

        [HttpPost("manage-store-rotation")]
        public async Task<IActionResult> ManageStoreRotation(string userId, string storeId)
        {
            var result = await _storeService.ManageStoreRotation(userId, storeId);
            return Ok(result);
        }

        [HttpGet("store-was-managed")]
        public async Task<IActionResult> StoreWasManaged(string storeId)
        {
            var result = await _storeService.StoreWasManaged(storeId);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateStore(StoreDTO storeDTO)
        {
            var result = await _storeService.UpdateStore(storeDTO);
            return Ok(result);
        }
    }
}
