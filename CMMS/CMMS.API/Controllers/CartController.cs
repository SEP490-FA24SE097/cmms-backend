using AutoMapper;
using Azure.Core;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.API.Services;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [AllowAnonymous]
    public class CartController : ControllerBase
    {
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IMapper _mapper;
        private ICartService _cartService;
        private ICurrentUserService _currentUserService;

        public CartController(ICartService cartService,
            ICurrentUserService currentUserService,
            IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService,
            IMapper mapper)
        {
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
            _mapper = mapper;
            _cartService = cartService;
            _currentUserService = currentUserService;
        }

        [HttpPost("getCart")]
        public async Task<IActionResult> GetUserCartAsync(CartItemRequest cartItems)
        {
            var listCartRequest = cartItems.CartItems.ToPageList(cartItems.currentPage, cartItems.perPage);
            List<CartItemVM> listCartItemVM = new List<CartItemVM>();
            foreach (var cartItem in listCartRequest)
            {
                CartItemVM cartItemVM = _mapper.Map<CartItemVM>(cartItem);
                var addItemModel = _mapper.Map<AddItemModel>(cartItem);
                var item = await _cartService.GetItemInStoreAsync(addItemModel);
                if (cartItem.Quantity > item.TotalQuantity)
                {
                    cartItemVM.IsChangeQuantity = true;
                }
                var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                cartItemVM.ItemName = material.Name;
                cartItemVM.BasePrice = material.SalePrice;
                cartItemVM.ImageUrl = material.ImageUrl;
                cartItemVM.ItemTotalPrice = material.SalePrice * cartItem.Quantity;
                if (cartItem.VariantId != null)
                {
                    var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).FirstOrDefault();
                    var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                    cartItemVM.ItemName += $" | {variantAttribute.Value}";
                    cartItemVM.BasePrice = variant.Price;
                    cartItemVM.ImageUrl = variant.VariantImageUrl;
                    cartItemVM.ItemTotalPrice = variant.Price * cartItem.Quantity;
                }
                listCartItemVM.Add(cartItemVM);
            }

            return Ok(new
            {
                data = listCartItemVM,
                pagination = new
                {
                    total = cartItems.CartItems.Count(),
                    perPage = cartItems.perPage,
                    currentPage = cartItems.currentPage,
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddItemToCart(CartItemModel model)
        {
            var addItemModel = _mapper.Map<AddItemModel>(model);
            var item = await _cartService.GetItemInStoreAsync(addItemModel);
            if (model.Quantity < item.TotalQuantity)
            {
                return Ok(new { success = true, message = "Thêm sản phẩm vào giỏ hàng thành công" });
            }
            return Ok(new { success = false, message = "Số lượng sản phẩm vượt quá số lượng trong kho" });
        }


    }
}
