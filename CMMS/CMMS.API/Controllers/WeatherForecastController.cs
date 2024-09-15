using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }
        [HasPermission(Permission.ViewDashboard)]
        [HttpGet(Name = "GetWeatherForecast")]
        public IActionResult Get()
        {
            return Ok(Summaries);
        }
    }
}
