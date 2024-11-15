﻿using AutoMapper;
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

        [HttpPost]
        public async Task<IActionResult> AddItemToCartAsync(CartItemModel model)
        {
            var addItemModel = _mapper.Map<AddItemModel>(model);
            var item =  await _cartService.GetItemInStoreAsync(addItemModel);
            if (model.Quantity <= item.TotalQuantity)
            {
                return Ok(new { success = true, message = "Thêm sản phẩm vào giỏ hàng thành công" });
            }
            return Ok(new { success = false, message = "Số lượng sản phẩm vượt quá số lượng trong kho" });
        }


    }
}
