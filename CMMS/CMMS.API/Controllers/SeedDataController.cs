using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedDataController : ControllerBase
    {
        private ApplicationDbContext _context;
        private RoleManager<IdentityRole> _roleManager;

        public SeedDataController(ApplicationDbContext context, 
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        [HttpGet(Name = "SeedingRole")]
        public async Task<IActionResult> SeedRole() {
            foreach (Role role in Enum.GetValues(typeof(Role))) {
                if (!await _roleManager.RoleExistsAsync(role.ToString())) {
                    await _roleManager.CreateAsync(new IdentityRole(role.ToString()));
                }
            }
            return Ok();
        }
    }
}
