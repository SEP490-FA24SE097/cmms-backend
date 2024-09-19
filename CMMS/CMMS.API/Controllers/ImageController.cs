using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImageController : Controller
    {
        private readonly IImageService _imageService;
        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string materialId,[FromBody] ImageDTO[] images)
        {
            try
            {
                var imageSet = new HashSet<Image>();
                foreach (var image in images)
                {
                    imageSet.Add(new Image
                    {
                        Id = new Guid(),
                        ImageUrl = image.ImageUrl,
                        MaterialId = Guid.Parse(materialId),
                        IsMainImage = image.IsMainImage
                    }
                    );
                }
                await _imageService.AddRange(imageSet);
                await _imageService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        [HttpDelete("delete-image")]
        public async Task<IActionResult> Delete([FromQuery] string imageId)
        {
            try
            {
                await _imageService.Remove(Guid.Parse(imageId));
                await _imageService.SaveChangeAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
