using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private IRoleService _roleSerivce;

        public AdminController(IRoleService roleService)
        {
            _roleSerivce = roleService;
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
