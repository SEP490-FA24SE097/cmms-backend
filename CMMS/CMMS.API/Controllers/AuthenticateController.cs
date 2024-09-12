using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    public class AuthenticateController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
