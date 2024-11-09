using CMMS.Core.Entities;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMMS.Core.Models;

namespace CMMS.API.Controllers
{
    [Route("api/sub-images")]
    [ApiController]
    public class SubImageController : ControllerBase
    {
        private readonly ISubImageService _subImageService;

        public SubImageController(ISubImageService subImageService)
        {
            _subImageService = subImageService;
        }




        [HttpGet("get-sub-images-by-material-id")]
        public ActionResult GetSubImage([FromQuery] Guid materialId)
        {
            var subImage = _subImageService.Get(x => x.MaterialId == materialId).ToList();

            return Ok(new { data = subImage });
        }

        // POST: api/SubImage
        [HttpPost]
        public async Task<ActionResult> CreateSubImage([FromBody] SubImageCM subImage)
        {
            try
            {
                await _subImageService.AddAsync(new SubImage
                {
                    Id = new Guid(),
                    MaterialId = subImage.MaterialId,
                    SubImageUrl = subImage.SubImageUrl
                }
                );
                await _subImageService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }


        }
        // DELETE: api/SubImage/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSubImage(Guid id)
        {
            try
            {
                await _subImageService.Remove(id);
                await _subImageService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }

        }
    }
}
