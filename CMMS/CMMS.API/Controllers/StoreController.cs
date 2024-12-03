using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CMMS.API.Controllers
{
    [Route("api/store")]
    [ApiController]
    [AllowAnonymous]
    public class StoreController : ControllerBase
    {
        private IStoreService _storeService;
        private readonly HttpClient _httpClient;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public StoreController(IStoreService storeService, HttpClient httpClient,
            IConfiguration configuration, IMapper mapper)
        {
            _storeService = storeService;
            _configuration = configuration;
            _httpClient = httpClient;
            _mapper = mapper;

        }

    #region CURD Store 
    [HttpGet]
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


        [HttpPost]
        public async Task<IActionResult> AddNewStore(StoreCM store)
        {
            var isDuplicatedName = _storeService.Get(_ => _.Name.Equals(store.Name)).FirstOrDefault();
            if (isDuplicatedName != null)
            {
                return BadRequest("Trùng tên store");
            }
            var address = $"{store.Address}, {store.Ward}, {store.District}, {store.Province}";
            var baseUrl = _configuration["GeoCodingAPI:BaseUrlGet"];
            var apiKey = _configuration["GeoCodingAPI:APIKey"];
            var apiUrl = $"{baseUrl}?q={address}&apiKey={apiKey}";
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Failed to fetch taxCode api checking");
            }
            var responseContent = await response.Content.ReadAsStringAsync();

            var jsonDocument = JsonDocument.Parse(responseContent);
            var position = jsonDocument.RootElement.GetProperty("items")[0].GetProperty("position");

            var lat = position.GetProperty("lat").ToString();
            var lng = position.GetProperty("lng").ToString();

            var storeEntity = _mapper.Map<Store>(store);
            storeEntity.Id = _storeService.GenerateStoreId();
            storeEntity.Latitude = lat;
            storeEntity.Longitude = lng;
            await _storeService.AddAsync(storeEntity);
            var result = await _storeService.SaveChangeAsync();
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

        #endregion
    }
}
