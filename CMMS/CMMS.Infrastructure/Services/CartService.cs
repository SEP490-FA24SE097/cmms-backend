using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface ICartService
    {
        Task AddAsync(Cart cart);
        Task<IdentityResult> AddToCart(CustomerAddItemToCartModel item);
        Task<IdentityResult> DecreaseQuantity(CustomerAddItemToCartModel item);
        Task<IdentityResult> UpdateItemQuantity(CustomerUpdateItemInCartModel updateItem);
        Task<IdentityResult> AddQuantityToCart(CustomerAddItemToCartModel addItem);
        List<CartVM> GetCartItemsByUserId(string userId);
        Task<IdentityResult> DeleteItemInCart(CustomerAddItemToCartModel deleteItem);
		Task<decimal> GetTotalAmountCart(string customerId);
	}
	public class CartService : ICartService
    {
        private IMapper _mapper;
        private ICartRepository _cartRepository;
        private IUnitOfWork _unitOfWork;
        private IUserService _userService;
        private IMaterialService _materialService;
        private IVariantService _variantService;

        public CartService(ICartRepository cartRepository, IUnitOfWork unitOfWork,
            IUserService userService, IMapper mapper,
            IMaterialService materialService, IVariantService variantService)
        {
            _mapper = mapper;
            _cartRepository = cartRepository;
            _unitOfWork = unitOfWork;
            _userService = userService;
            _materialService = materialService;
            _variantService = variantService;


        }
        public bool IsAvailableQuantity(string materialId, string? variantId)
        {
            // logic to check quantity in warehouse.
            return true;
        }
        public async Task<IdentityResult> AddQuantityToCart(CustomerAddItemToCartModel addItem)
        {

            // 2 truong hop 
            // neu mua material khong 
            // va neu mua material va ca variant nua.
            // thi check id phai nhu the nao???
            // thong qua addItem
            // get variant or material 
            //var existingCartItem = _cartRepository.Get(_ => _.CustomerId.Equals(addItem.CustomerId)
            //&& _.MaterialId.Equals(addItem.MaterialId));

            var existingCartItem = _cartRepository.Get(_ => _.CustomerId.Equals(addItem.CustomerId)
            && _.MaterialId.Equals(addItem.MaterialId));

            if (addItem.VariantId != null)
            {
                existingCartItem.Where(_ => _.VariantId.Equals(addItem.VariantId));
            }
            var cartItem = existingCartItem.FirstOrDefault();
            //var cartItem = existingCartItem.FirstOrDefault();
			if (cartItem != null)
            {
                // get quantity cua item o store.
                //if (items.Quantity == existingCartItem.Quantity)
                //{
                //    return IdentityResult.Failed(new IdentityError { Description = "Quantity not enough!!!" });
                //}
                // Item exists, update the quantity
                var oldQuantity = cartItem.Quantity;
                var itemPrice = cartItem.TotalAmount / oldQuantity;
				var newQuantity = oldQuantity + 1;
                cartItem.TotalAmount = itemPrice * newQuantity;
                cartItem.Quantity += 1;
                cartItem.UpdateAt = DateTime.UtcNow;
                _cartRepository.Update(cartItem);
            }
            else
            {
				var material = _materialService.Get(_ => _.Id.Equals(Guid.Parse(addItem.MaterialId))).FirstOrDefault();
				var totalAmount = material.SalePrice;
				if (addItem.VariantId != null)
				{
					var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(addItem.VariantId))).FirstOrDefault();
                    existingCartItem.Where(_ => _.VariantId.Equals(addItem.VariantId));
                    totalAmount = variant.Price;
				}
				// Item does not exist, add a new CartItem
				var newCartItem = new Cart
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = addItem.CustomerId,
                    MaterialId = Guid.Parse(addItem.MaterialId),
                    VariantId = addItem.VariantId != null ? Guid.Parse(addItem.VariantId) : null,
                    TotalAmount = totalAmount,
                    Quantity = 1,
                    UpdateAt = DateTime.UtcNow,
                    CreateAt = DateTime.UtcNow,
                };
                await _cartRepository.AddAsync(newCartItem);
            }
            var saveResult = await _unitOfWork.SaveChangeAsync();
            if (saveResult)
            {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(new IdentityError { Description = "Could not save changes to the database." });
        }

        public Task<IdentityResult> AddToCart(CustomerAddItemToCartModel item)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> DecreaseQuantity(CustomerAddItemToCartModel item)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> DeleteItemInCart(CustomerAddItemToCartModel deleteItem)
        {
            throw new NotImplementedException();
        }

        public List<CartVM> GetCartItemsByUserId(string userId)
        {
            List<CartVM> listUserCart = new List<CartVM>();
            var userCart = _cartRepository.Get(_ => _.CustomerId.Equals(userId)).ToList();
            if (!userCart.IsNullOrEmpty())
            {
                listUserCart = _mapper.Map<List<CartVM>>(userCart);
                foreach (var item in listUserCart)
                {
                    var material = _materialService.Get(_ => _.Id.Equals(Guid.Parse(item.MaterialId))).FirstOrDefault();
                    var imageUrl = material.ImageUrl;
                    var totalAmount = material.SalePrice * item.Quantity;

					if (item.VariantId != null)
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
                        imageUrl = variant.VariantImageUrl;
						totalAmount = variant.Price * item.Quantity;
					}
                    item.TotalAmount = (double)totalAmount;
					item.ImageUrl = imageUrl;
                }
                return listUserCart;
            }
            return null;
        }

        public Task<IdentityResult> UpdateItemQuantity(CustomerUpdateItemInCartModel updateItem)
        {
            throw new NotImplementedException();
        }

        public async Task AddAsync(Cart cart)
        {
           await _cartRepository.AddAsync(cart);
        }

		public async Task<decimal> GetTotalAmountCart(string customerId)
		{
            return await _cartRepository.Get(_ => _.CustomerId.Equals(customerId)).SumAsync(_ => _.TotalAmount);
		}
	}
}
