﻿using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Models;
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
    [ApiController]
    public class AdminController : ControllerBase
    {
        private IUserService _userService;
        private IRoleService _roleSerivce;
        private IPermissionSerivce _permissionService;
        private IMapper _mapper;

        public AdminController(IRoleService roleService,
            IPermissionSerivce permissionSerivce,
            IUserService userSerivce, IMapper mapper)
        {
            _userService = userSerivce;
            _roleSerivce = roleService;
            _permissionService = permissionSerivce;
            _mapper = mapper;
        }
        #region userManagement
        [HasPermission(Permission.StoreMaterialTracking)]
        [HttpGet("get-all-user")]
        public async Task<IActionResult> GetAllUser([FromQuery] DefaultSearch defaultSearch)
        {
            var result = await _userService.GetAll();
            var data = result.Sort(string.IsNullOrEmpty(defaultSearch.sortBy) ? "UserName" : defaultSearch.sortBy
                      , defaultSearch.isAscending)
                      .ToPageList(defaultSearch.currentPage, defaultSearch.perPage).AsNoTracking().ToList();
            return Ok(new { total = result.ToList().Count, data, page = defaultSearch.currentPage, perPage = defaultSearch.perPage });
        }


        [HttpPost("add-new-user")]
        public async Task<IActionResult> AddNewUser(UserCM userCM)
        {
            var result = await _userService.AddAsync(userCM);
            return Ok(result);
        }
        #endregion
        #region permissions
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



        #region seeding
        [AllowAnonymous]
        [HttpGet("SeedRole")]
        public IActionResult SeedRole()
        {
            try
            {
                _roleSerivce.SeedingRole();
            }
            catch (Exception)
            {
                throw;
            }
            return Ok();
        }
        [AllowAnonymous]
        [HttpGet("SeedPermission")]
        public IActionResult SeedPermission()
        {
            try
            {
                _roleSerivce.SeedingPermission();
            }
            catch (Exception)
            {
                throw;
            }
            return Ok();
        }
        [AllowAnonymous]
        [HttpGet("SeedRolePermission")]
        public IActionResult SeedingRolePermission()
        {
            try
            {
                _roleSerivce.LinkRolePermission();
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
