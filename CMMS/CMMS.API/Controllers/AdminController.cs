using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
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

        [HttpGet("GetAllPermission")]
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
        [HttpGet("GetUserPermission")]
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
        [HttpGet("GetRolePermission")]
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

        [HttpPost("AddNewUserPermission")]
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

        [HttpDelete("RemoveUserPermission")]
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

    }
}
