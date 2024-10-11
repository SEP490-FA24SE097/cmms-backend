using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/units")]
    public class UnitController : Controller
    {
        private readonly IUnitService _unitService;
        public UnitController(IUnitService unitService)
        {
            _unitService = unitService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            try
            {
                var result = _unitService.GetAll().Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnitById([FromRoute] string id)
        {
            try
            {
                var result = await _unitService.FindAsync(Guid.Parse(id));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<string> units)
        {
            try
            {

                await _unitService.AddRange(units.Select(x => new Unit
                {
                    Id = new Guid(),
                    Name = x
                }));
                await _unitService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UnitUM unitUM)
        {
            try
            {
                if (_unitService.Get(c => c.Name.Contains(unitUM.Name)).Any())
                {
                    return BadRequest(new { success = false, message = "Unit name is already existed !" });
                }
                var result = await _unitService.FindAsync(unitUM.Id);
                result.Name = unitUM.Name;
                _unitService.Update(result);
                await _unitService.SaveChangeAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpDelete("delete-unit")]
        public async Task<IActionResult> Update([FromQuery] string unitId)
        {
            try
            {
                await _unitService.Remove(Guid.Parse(unitId));
                await _unitService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
