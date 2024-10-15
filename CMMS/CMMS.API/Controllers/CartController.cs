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

		public CartController(ICartService cartService,
			ICurrentUserService currentUserService)
		{
			_cartService = cartService;
			_currentUserService = currentUserService;
		}
		[HttpGet]
		public IActionResult GetCustomerCart()
		{
			var userId = _currentUserService.GetUserId();
			if (userId == null) return Unauthorized();
			var result = _cartService.GetCartItemsByUserId(userId);
			if (result == null) return NotFound("Your cart is empty");
			return Ok(new
			{
				total = result.Count,
				data = result,
			});
		}

		[HttpPost]
		public async Task<IActionResult> AddItemToCart(AddItemModel model)
		{
			var userId = _currentUserService.GetUserId();
			if (userId == null) return Unauthorized();
			CustomerAddItemToCartModel addItem = new CustomerAddItemToCartModel
			{
				CustomerId = userId,
				MaterialId = model.MaterialId,
				VariantId = model.VariantId,
			};

			var result = await _cartService.AddQuantityToCart(addItem);
			if (result.Succeeded)
			{
				return Ok("Add this item to cart successfully");
			}
			return BadRequest($"Add this item to {userId}'s cart failed");
		}


	}
}
