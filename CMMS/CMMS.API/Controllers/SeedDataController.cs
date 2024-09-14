using CMMS.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedDataController : ControllerBase
    {
        private ApplicationDbContext _context;

        public SeedDataController(ApplicationDbContext context)
        {
            _context = context;
        }

        []
    }
}
