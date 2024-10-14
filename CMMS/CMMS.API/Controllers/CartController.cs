using CMMS.API.Services;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [AllowAnonymous]
    public class CartController : ControllerBase
    {
        private ICartService _cartService;
        private ICurrentUserService _currentUserService;

        public CartController(ICartService cartService, ICurrentUserService currentUserService)
        {
            _cartService = cartService; 
            _currentUserService = currentUserService;   
        }
        [HttpGet]
        public IActionResult GetCustomerCart() { 
            var userId = _currentUserService.GetUserId();
            if (userId != null) return Unauthorized();
            var result = _cartService.GetCartItemsByUserId(userId);
            if (result == null) return NotFound();  
            return Ok(result);  
        }

		[HttpGet]
		public IActionResult AddItemToCart(AddItemDTO addItem)
		{
			var userId = _currentUserService.GetUserId();
			if (userId != null) return Unauthorized();
			var result = _cartService.GetCartItemsByUserId(userId);
			if (result == null) return NotFound();
			return Ok(result);
		}


	}
}
