using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/suppliers")]
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;
        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            try
            {
                var result = _supplierService.GetAll().Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    Address = x.Address,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email
                });
                return Ok(new{data=result});
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplierById([FromRoute] string id)
        {
            try
            {
                var result = await _supplierService.FindAsync(Guid.Parse(id));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SupplierDTO supplierDTO)
        {
            try
            {

                await _supplierService.AddAsync(new Supplier
                {
                    Id = new Guid(),
                    Address = supplierDTO.Address,
                    Email = supplierDTO.Email,
                    PhoneNumber = supplierDTO.PhoneNumber,
                    Name = supplierDTO.Name

                });
                await _supplierService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromQuery] string supplierId, [FromBody] SupplierDTO supplierDTO)
        {
            try
            {
                var result = await _supplierService.FindAsync(Guid.Parse(supplierId));
                result.Name = supplierDTO.Name.IsNullOrEmpty() ? result.Name : supplierDTO.Name;
                result.Email = supplierDTO.Email.IsNullOrEmpty() ? result.Email : supplierDTO.Email;
                result.PhoneNumber = supplierDTO.PhoneNumber.IsNullOrEmpty() ? result.PhoneNumber : supplierDTO.PhoneNumber;
                result.Address = supplierDTO.Address.IsNullOrEmpty() ? result.Address : supplierDTO.Address;
                _supplierService.Update(result);
                await _supplierService.SaveChangeAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpDelete("delete-supplier")]
        public async Task<IActionResult> Delete([FromQuery] string supplierId)
        {
            try
            {
                await _supplierService.Remove(Guid.Parse(supplierId));
                await _supplierService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
