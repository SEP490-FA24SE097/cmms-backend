using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities.Configurations;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CMMS.API.Controllers
{
    [Route("api/admin")]
    [AllowAnonymous]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private IInvoiceService _invoiceService;
        private IUserService _userService;
        private IRoleService _roleSerivce;
        private IPermissionSerivce _permissionService;
        private IMapper _mapper;
        private IConfigurationCustomerDiscountService _configCustomerDiscountService;
        private IConfigurationShippingServices _configShippingService;
        private IStoreService _storeService;
        private ITransaction _efTransaction;
        private IStoreInventoryService _storeInventoryService;

        public AdminController(IRoleService roleService,
            IPermissionSerivce permissionSerivce,
            IUserService userSerivce, IMapper mapper, IConfigurationCustomerDiscountService configCustomerDiscountService,
            IConfigurationShippingServices configShippingService, IInvoiceService invoiceService, IStoreService storeService,
            ITransaction transaction, IStoreInventoryService storeInventoryService)
        {
            _invoiceService = invoiceService;
            _userService = userSerivce;
            _roleSerivce = roleService;
            _permissionService = permissionSerivce;
            _mapper = mapper;
            _configCustomerDiscountService = configCustomerDiscountService;
            _configShippingService = configShippingService;
            _storeService = storeService;
            _efTransaction = transaction;
            _storeInventoryService = storeInventoryService;
        }
        #region userManagement
        [HasPermission(PermissionName.SeniorPermission)]
        [HttpGet("get-all-user")]
        public async Task<IActionResult> GetAllUser([FromQuery] DefaultSearch defaultSearch)
        {
            var result = await _userService.GetAll();
            var data = result.Sort(string.IsNullOrEmpty(defaultSearch.sortBy) ? "UserName" : defaultSearch.sortBy
                      , defaultSearch.isAscending)
                      .ToPageList(defaultSearch.currentPage, defaultSearch.perPage).AsNoTracking().ToList();
            return Ok(new { total = result.ToList().Count, data, page = defaultSearch.currentPage, perPage = defaultSearch.perPage });
        }

        [HasPermission(PermissionName.SeniorPermission)]
        [HttpPost("add-new-user")]
        public async Task<IActionResult> AddNewUser(UserCM userCM)
        {
            var result = await _userService.AddAsync(userCM);
            return Ok(result);
        }
        #endregion
        #region permissions
        [HasPermission(PermissionName.SeniorPermission)]
        [HttpGet("get-all-permissions")]
        public IActionResult GetAllPermission([FromQuery] DefaultSearch defaultSearch)
        {
            var permissions = _permissionService.GetAll();
            var result = permissions.Sort(string.IsNullOrEmpty(defaultSearch.sortBy) ? "Name" : defaultSearch.sortBy
                      , defaultSearch.isAscending)
                      .ToPageList(defaultSearch.currentPage, defaultSearch.perPage).AsNoTracking().ToList();
            return Ok(new
            {
                data = result,
                meta = new
                {
                    total = permissions.Count(_ => _.Id != null),
                    perPage = defaultSearch.perPage,
                    currentPage = defaultSearch.currentPage,
                }
            }
            );
        }
        [HttpGet("get-user-permissions/{userId}")]
        [HasPermission(PermissionName.SeniorPermission)]
        public async Task<IActionResult> GetUserPermisison(string userId)
        {
            var user = _userService.Get(u => u.Id.Equals(userId)).FirstOrDefault();
            if (user == null) return Ok(new ResponseError
            {
                error = new ErrorDTO
                {
                    code = "404",
                    message = "User Not Found"
                }
            });
            var userVM = _mapper.Map<UserVM>(user);
            var result = await _permissionService.GetUserPermission(userId);
            var response = new Response
            {
                data = new
                {
                    user = userVM,
                    permissions = result
                }
            };
            return Ok(response);

        }
        [HttpGet("get-role-permissions/{roleId}")]
        [HasPermission(PermissionName.SeniorPermission)]
        public async Task<IActionResult> GetRolePermisison(string roleId)
        {
            var role = await _roleSerivce.GetRoleById(roleId);
            if (role == null) return Ok(new ResponseError
            {
                error = new ErrorDTO
                {
                    code = "404",
                    message = "Role Not Exist"
                }
            });
            var result = await _permissionService.GetRolePermision(roleId);
            var response = new Response
            {
                data = result,
            };
            return Ok(response);
        }

        [HttpPost("add-new-user-permission")]
        [HasPermission(PermissionName.SeniorPermission)]
        public async Task<IActionResult> AddNewUserPermission(UserPermissionDTO userPermissionDTO)
        {
            if (ModelState.IsValid)
            {
                var result = await _permissionService
                    .AddUserPermission(userPermissionDTO.UserId, userPermissionDTO.PermissionId);
                if (!result)
                {
                    return Ok(new ResponseError
                    {
                        error = new ErrorDTO
                        {
                            code = "400",
                            message = "Cannot add user permission"
                        }
                    });
                }
                return Ok();
            }
            else
            {
                return Ok(new ResponseError
                {
                    error = new ErrorDTO
                    {
                        code = "400",
                        message = "UserId and PermissionId have to not null"
                    }
                });
            }
        }

        [HttpDelete("remove-user-permission")]
        [HasPermission(PermissionName.SeniorPermission)]
        public async Task<IActionResult> RemoveUserPermission(UserPermissionDTO userPermissionDTO)
        {
            if (ModelState.IsValid)
            {
                var result = await _permissionService
                    .RemoveUserPermission(userPermissionDTO.UserId, userPermissionDTO.PermissionId);
                if (!result)
                {
                    return BadRequest("Cannot add user permission");
                }
                return Ok();
            }
            else
            {
                return BadRequest("UserId and Permission not null");
            }
        }

        #endregion

        #region roles

        [HttpGet("get-roles")]
        public async Task<IActionResult> GetAccounts()
        {
            var result = await _roleSerivce.GetRole();
            var listRoles = result.Select(_ => new
            {
                _.Id,
                _.Name
            });
            return Ok(listRoles);
        }

        [HttpGet("get-role/{id}")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var result = await _roleSerivce.GetRoleById(id);
            if (result != null) { return Ok(result); }
            return BadRequest("Cannot found");
        }

        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var result = await _roleSerivce.CreateRole(roleName);
            return Ok(result);
        }

        [HttpPut("update-role/{id}")]
        public async Task<IActionResult> UpdateRole(string roleName, string id)
        {
            var result = await _roleSerivce.UpdateRole(roleName, id);
            if (result > 0) return Ok();
            return BadRequest("Cannot update");
        }

        [HttpDelete("delete-role/{roleId}")]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var result = await _roleSerivce.DeleteRole(roleId);
            return Ok(result);
        }

        [HttpGet("get-user-role/{userId}")]
        public async Task<IActionResult> GetUserRole(string userId)
        {
            var result = await _roleSerivce.GetUserRole(userId);
            if (result != null) return Ok(result);
            return BadRequest("Cannot found");
        }


        [HttpPost("add-user-to-role")]
        public async Task<IActionResult> AddRoleUser(List<string> roleNames, string userId)
        {
            var result = await _roleSerivce.AddRoleUser(roleNames, userId);
            return Ok(result);
        }



        #endregion


        #region Cofigurations data
        [HttpGet("shipping-free-config")]
        [HasPermission(PermissionName.SeniorPermission)]
        public IActionResult GetShippingFreeConfiguration([FromQuery] ShippingConfigurationFilterModel filterModel)
        {
            var filterList = _configShippingService.Get(_ =>
                   (!filterModel.FromDate.HasValue || _.CreatedAt >= filterModel.FromDate) &&
                   (!filterModel.ToDate.HasValue || _.CreatedAt <= filterModel.ToDate));
            var total = filterList.Count();
            var filterListPaged = filterList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage);
            return Ok(new
            {
                data = filterListPaged.OrderByDescending(_ => _.CreatedAt),
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }

        [HttpPost("add-shipping-free-config")]
        [HasPermission(PermissionName.SeniorPermission)]
        public async Task<IActionResult> AddShippingFreeConfiguration(ShippingCofigDTO model)
        {
            try
            {
                var shippingConfig = _mapper.Map<ConfigShipping>(model);
                shippingConfig.Id = Guid.NewGuid().ToString();
                shippingConfig.CreatedAt = Helpers.TimeConverter.GetVietNamTime();
                await _configShippingService.AddAsync(shippingConfig);
                var result = await _configShippingService.SaveChangeAsync();
                if (result)
                {
                    await _efTransaction.CommitAsync();
                    return Ok("Tạo mới cấu hình giá tiền ship thành công");


                }
                return BadRequest("Không thể tạo mới cấu hình");
            }
            catch (Exception)
            {
                return BadRequest("Không thể tạo mới cấu hình");
            }

        }



        [HttpGet("get-revenue-all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRevue()
        {

            var result = await _invoiceService.GetStoreMonthlyRevenueAsync();
            var months = Enumerable.Range(1, 12);

            var allStores = _storeService.GetAll()
             .Select(store => new { store.Id, store.Name })
             .ToList();

            // get revenue in all store.
            var groupedData = result
                .GroupBy(x => x.Year)
                .ToDictionary(
                    yearGroup => yearGroup.Key,
                    yearGroup => allStores
                        .ToDictionary(
                            store => store.Name,
                            store => {
                                var storeData = yearGroup
                                    .Where(x => x.StoreId == store.Id)
                                    .ToList();
                                return months
                                    .Select(month => storeData.FirstOrDefault(x => x.Month == month)?.MonthlyRevenue ?? 0)
                                    .ToArray();
                            }
                        )
                );


            return Ok(new
            {
                groupedData,
            });
        }

        [HttpGet("get-top-product")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInvoiceRevue()
        {
            var result = await _invoiceService.GetStoreMonthlyRevenueAsync();
            var months = Enumerable.Range(1, 12);
        

            return Ok();
        }


        [HttpGet("customer-type-discount-config")]
        [HasPermission(PermissionName.SeniorPermission)]
        public IActionResult GetCustomerDiscountConfiguration([FromQuery] CustomerDiscountConfigurationFilterModel filterModel)
        {
            var filterList = _configCustomerDiscountService.Get(_ =>
                   (!filterModel.FromDate.HasValue || _.CreatedAt >= filterModel.FromDate) &&
                   (!filterModel.ToDate.HasValue || _.CreatedAt <= filterModel.ToDate));
            var total = filterList.Count();
            var filterListPaged = filterList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage);
            return Ok(new
            {
                data = filterListPaged.OrderByDescending(_ => _.CreatedAt),
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }

        [HttpPost("customer-type-discount-config")]
        [HasPermission(PermissionName.SeniorPermission)]
        public async Task<IActionResult> AddCustomerDiscountConfiguration(CustomerDiscountCofigDTO model)
        {
            try
            {
                var customerDiscount = _mapper.Map<ConfigCustomerDiscount>(model);
                customerDiscount.Id = Guid.NewGuid().ToString();
                customerDiscount.CreatedAt = Helpers.TimeConverter.GetVietNamTime();
                await _configCustomerDiscountService.AddAsync(customerDiscount);
                var result = await _configCustomerDiscountService.SaveChangeAsync();
                if (result)
                {
                    return Ok("Tạo mới cấu hình giảm giá cho khách hàng thành công");
                }
                return BadRequest("Không thể tạo mới cấu hình");
            }
            catch (Exception)
            {
                return BadRequest("Không thể tạo mới cấu hình");
            }

        }

        #endregion


        #region seeding
        [HasPermission(PermissionName.SeniorPermission)]
        [HttpGet("SeedRole")]
        public async Task<IActionResult> SeedRoleAsync()
        {
            try
            {
                await _roleSerivce.SeedingRole();
            }
            catch (Exception)
            {
                throw;
            }
            return Ok();
        }
        [HasPermission(PermissionName.SeniorPermission)]
        [HttpGet("SeedPermission")]
        public async Task<IActionResult> SeedPermissionAsync()
        {
            try
            {
                await _roleSerivce.SeedingPermission();
            }
            catch (Exception)
            {
                throw;
            }
            return Ok();
        }
        [HasPermission(PermissionName.SeniorPermission)]
        [HttpGet("SeedRolePermission")]
        public async Task<IActionResult> SeedingRolePermissionAsync()
        {
            try
            {
                await _roleSerivce.LinkRolePermission();
            }
            catch (Exception)
            {
                throw;
            }
            return Ok();
        }
        #endregion


    }
}
