using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Repositories;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CMMS.API.Controllers
{
    [Route("api/store")]
    [ApiController]
    [AllowAnonymous]
    public class StoreController : ControllerBase
    {
        private IStoreService _storeService;
        private IMailService _mailService;
        private ITransaction _efTransaction;
        private ICurrentUserService _currentUserService;
        private readonly HttpClient _httpClient;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public StoreController(IStoreService storeService, HttpClient httpClient,
            IConfiguration configuration, IMapper mapper, IUserService userService,
            IMailService mailService, ITransaction efTransaction, ICurrentUserService currentUserService)
        {
            _storeService = storeService;
            _configuration = configuration;
            _httpClient = httpClient;
            _mapper = mapper;
            _userService = userService;
            _mailService = mailService;
            _efTransaction = efTransaction;
            _currentUserService = currentUserService;

        }

        #region CURD Store 
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DefaultSearch defaultSearch, StoreType storeType)
        {
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
            if (result == null) return NotFound("Store not found");
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
        [HttpPut("get-store-manager")]
        public async Task<IActionResult> GéttoreManager(StoreDTO storeDTO)
        {
            var result = await _storeService.UpdateStore(storeDTO);
            return Ok(result);
        }

        [HttpPut("add-store-manager")]
        public async Task<IActionResult> AddStoreManager(StoreDTO storeDTO)
        {
            var result = await _storeService.UpdateStore(storeDTO);
            return Ok(result);
        }


        [HttpPost("add-staff")]
        public async Task<IActionResult> AddNewStaff(UserDTO model)
        {
            var currentUserId = _currentUserService.GetUserId();
            model.CreatedById = currentUserId;
            var result = new Core.Constant.Message();
            if (model.StaffRole == (int)Role.Sale_Staff)
            {
                result = await _storeService.AddNewSaleStaffAsync(model);
            }
            else if (model.StaffRole == (int)Role.Store_Manager)
            {
                result = await _storeService.AddNewStoreManagerAsync(model);
            }
            else if (model.StaffRole == (int)Role.Shipper_Store)
            {
                result = await _storeService.AddNewShipperAsync(model);
            }
            if (result.StatusCode == 200)
            {
                await _mailService.SendEmailAsyncStaff(model.Email, "Tài khoản cho hệ thống CMMS", null, model.UserName, model.Password);
                await _efTransaction.CommitAsync();
                return Ok(result.Content);
            }
                return BadRequest(result.Content);
            
        }

        [HttpGet("get-staff")]
        public async Task<IActionResult> GetStaff([FromQuery] StaffFilterModel filterModel)
        {
            var fitlerList = _userService
            .Get(_ =>
            (string.IsNullOrEmpty(filterModel.Name) || _.FullName.Contains(filterModel.Name)) &&
            (string.IsNullOrEmpty(filterModel.StoreId) || _.StoreId.Equals(filterModel.StoreId)) &&
            (string.IsNullOrEmpty(filterModel.Email) || _.Email.Equals(filterModel.Email)) &&
            _.StoreId != null);

            if (filterModel.RoleStaff == (int)Role.Sale_Staff)
            {
                fitlerList = fitlerList.Where(_ => _.Id.Contains("NVBH"));
            }
            else if (filterModel.RoleStaff == (int)Role.Store_Manager)
            {
                fitlerList = fitlerList.Where(_ => _.Id.Contains("STM"));
            }
            else if (filterModel.RoleStaff == (int)Role.Shipper_Store)
            {
                fitlerList = fitlerList.Where(_ => _.Id.Contains("NVVC"));
            }

            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
               .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);

            var result = new List<StoreStaffVM>();
            foreach (var staff in filterListPaged)
            {
                var storesStaffVM = new StoreStaffVM();
                var createBy = _userService.Get(_ => _.Id.Equals(staff.CreatedById)).FirstOrDefault();
                var store = await _storeService.GetStoreById(staff.StoreId);
                storesStaffVM.StoreName = store.Name;
                storesStaffVM.Id = staff.Id;
                storesStaffVM.Email = staff.Email;
                storesStaffVM.RoleName = (await _userService.GetRolesAsync(staff)).FirstOrDefault();
                storesStaffVM.FullName = staff.FullName;
                storesStaffVM.DOB = staff.DOB.ToString();
                storesStaffVM.CreateById = staff.CreatedById;
                storesStaffVM.CreateBy = createBy.FullName;
                storesStaffVM.StoreId = store.Id;

                result.Add(storesStaffVM);
            }
            return Ok(new
            {
                data = result,
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });

        }


        [HttpGet("get-staff-id")]
        public async Task<IActionResult> GetStaffById(string id)
        {
            var user = _userService.Get(_ => _.Id.Equals(id)).FirstOrDefault();
            if(user == null)
            {
                return BadRequest("Không tìm thấy nhân viên này trên hệ thống");
            }
            var result = _mapper.Map<UserStoreVM>(user);
  
            return Ok(new { data = result });
        }

    }
}
