﻿using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        
        public AdminController()
        {
            
        }
        [HttpGet(Name = "SeedPermission")]
        public Task<IActionResult> Get() {

            return Ok("Create sucessfully");
        }

    }
}
