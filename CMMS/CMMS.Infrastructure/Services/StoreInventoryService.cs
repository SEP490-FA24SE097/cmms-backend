﻿using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CMMS.Infrastructure.Repositories;
using AutoMapper;
using CMMS.Infrastructure.Enums;
using Microsoft.EntityFrameworkCore;
using CMMS.Infrastructure.Constant;

namespace CMMS.Infrastructure.Services
{
    public interface IStoreInventoryService
    {
        #region CRUD
        Task<StoreInventory> FindAsync(Guid id);
        IQueryable<StoreInventory> GetAll();
        IQueryable<StoreInventory> Get(Expression<Func<StoreInventory, bool>> where);
        IQueryable<StoreInventory> Get(Expression<Func<StoreInventory, bool>> where, params Expression<Func<StoreInventory, object>>[] includes);
        IQueryable<StoreInventory> Get(Expression<Func<StoreInventory, bool>> where, Func<IQueryable<StoreInventory>, IIncludableQueryable<StoreInventory, object>> include = null);
        Task AddAsync(StoreInventory inventory);
        Task AddRange(IEnumerable<StoreInventory> inventories);
        void Update(StoreInventory inventory);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<StoreInventory, bool>> where);
        Task<bool> SaveChangeAsync();

        #endregion
        Task<StoreInventory> GetItemInStoreAsync(AddItemModel itemModel);
        Task<bool> CanPurchase(CartItem cartItem);
        Task<bool> UpdateStoreInventoryAsync(CartItem cartItem, int invoiceStatus);
        Task<decimal> GetAvailableQuantityInStore(CartItem cartItem);
        Task<List<PreCheckOutItemCartModel>> DistributeItemsToStores(CartItemRequest cartItems, List<StoreDistance> listStoreByDistance);
    }

    public class StoreInventoryService : IStoreInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreInventoryRepository _inventoryRepository;
        private readonly IMapper _mapper;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;

        public StoreInventoryService(IUnitOfWork unitOfWork, IStoreInventoryRepository 
            inventoryRepository, IVariantService variantService,
            IMapper mapper, IMaterialService materialService)
        {
            _unitOfWork = unitOfWork;
            _inventoryRepository = inventoryRepository;
            _mapper = mapper;
            _variantService = variantService;
            _materialService = materialService;
        }
        private async Task<StoreInventory?> GetStoreInventoryItem(Guid materialId, Guid? variantId, string storeId)
        {
            if (variantId == null)
                return await Get(x => x.MaterialId == materialId && x.VariantId == variantId && x.StoreId == storeId).FirstOrDefaultAsync();
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return await Get(x => x.MaterialId == materialId && x.VariantId == variantId && x.StoreId == storeId).FirstOrDefaultAsync();
                else
                {
                    return await Get(x => x.VariantId == variant.AttributeVariantId && x.StoreId == storeId).FirstOrDefaultAsync();
                }
            }
        }
        private async Task<decimal?> GetConversionRate(Guid materialId, Guid? variantId)
        {
            if (variantId == null)
                return null;
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).Include(x => x.ConversionUnit).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return null;

                else
                {
                    return variant.ConversionUnit.ConversionRate;
                }
            }
        }
        public async Task<StoreInventory> GetItemInStoreAsync(AddItemModel itemModel)
        {
            StoreInventory storeInventory = null;
            if (itemModel.VariantId != null)
            {
                var materialId = Guid.Parse(itemModel.MaterialId);
                var variantId = Guid.Parse(itemModel.VariantId);
                //  storeInventory = await _inventoryRepository.Get(x =>
                //x.StoreId.Equals(itemModel.StoreId)
                //&& x.MaterialId.Equals(materialId)
                //&& x.VariantId.Equals(variantId)).FirstOrDefaultAsync();
                storeInventory = await GetStoreInventoryItem(materialId, variantId, itemModel.StoreId);
            }
            else
            {
                var materialId = Guid.Parse(itemModel.MaterialId);
                storeInventory = await _inventoryRepository.Get(x =>
              x.StoreId.Equals(itemModel.StoreId) &&
              x.MaterialId.Equals(materialId) &&
              x.VariantId == null).FirstOrDefaultAsync();
            }
            return storeInventory;
        }
        public async Task<bool> CanPurchase(CartItem cartItem)
        {
            var item = _mapper.Map<AddItemModel>(cartItem);
            var storeInventory = await GetItemInStoreAsync(item);
            var conversionRate = await GetConversionRate(storeInventory.MaterialId, storeInventory.VariantId);
            if (storeInventory != null)
            {
                var availableQuantity = storeInventory.TotalQuantity - storeInventory.InOrderQuantity;
                var orderQuantity = conversionRate == null ? cartItem.Quantity : cartItem.Quantity * conversionRate;
                if (orderQuantity <= availableQuantity) return true;
                //if (cartItem.Quantity <= availableQuantity) return true;
            }
            return false;
        }
        public async Task<bool> UpdateStoreInventoryAsync(CartItem cartItem, int invoiceStatus)
        {
            var item = _mapper.Map<AddItemModel>(cartItem);
            var storeInventory = await GetItemInStoreAsync(item);
            var conversionRate = await GetConversionRate(storeInventory.MaterialId, storeInventory.VariantId);
            var orderQuantity = conversionRate == null ? cartItem.Quantity : cartItem.Quantity * conversionRate;
            if (storeInventory != null)
            {
                switch (invoiceStatus)
                {
                    case (int)InvoiceStatus.Pending:
                        // storeInventory.InOrderQuantity += cartItem.Quantity;
                        storeInventory.InOrderQuantity += orderQuantity;
                        break;
                    case (int)InvoiceStatus.Done:
                        // storeInventory.TotalQuantity -= cartItem.Quantity;
                        // storeInventory.InOrderQuantity -= cartItem.Quantity;
                        storeInventory.TotalQuantity -= (decimal)orderQuantity;
                        storeInventory.InOrderQuantity -= orderQuantity;
                        break;
                    case (int)InvoiceStatus.Cancel:
                    case (int)InvoiceStatus.Refund:
                        //storeInventory.TotalQuantity += cartItem.Quantity;
                        storeInventory.TotalQuantity += (decimal)orderQuantity;
                        //storeInventory.InOrderQuantity += cartItem.Quantity;
                        break;
                }
                _inventoryRepository.Update(storeInventory);
                return true;
            }
            return false;
        }

        public async Task<decimal> GetAvailableQuantityInStore(CartItem cartItem)
        {
            var item = _mapper.Map<AddItemModel>(cartItem);
            var storeInventory = await GetItemInStoreAsync(item);
            if (storeInventory != null) {
                var conversionRate = await GetConversionRate(storeInventory.MaterialId, storeInventory.VariantId);
                if (storeInventory != null)
                {
                    var availableQuantity = storeInventory.TotalQuantity - storeInventory.InOrderQuantity;
                    return (decimal)availableQuantity;
                }
            }
            return 0;
        }

        public async Task<List<PreCheckOutItemCartModel>> DistributeItemsToStores(CartItemRequest cartItems, List<StoreDistance> listStoreByDistance)
        {
            var result = new List<PreCheckOutItemCartModel>();

            foreach (var cartItem in cartItems.CartItems)
            {
                var remainingQuantity = cartItem.Quantity;

                foreach (var store in listStoreByDistance)
                {
                    var storeItem = _mapper.Map<CartItem>(cartItem);
                    storeItem.StoreId = store.Store.Id;
                    // Kiểm tra số lượng tồn kho của sản phẩm tại cửa hàng
                    var availableQuantity = await GetAvailableQuantityInStore(storeItem);

                    if (availableQuantity == 0) continue;

                    // Số lượng thực tế phân bổ cho cửa hàng
                    var allocatedQuantity = Math.Min(remainingQuantity, availableQuantity);

                    // Lấy thông tin sản phẩm
                    var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                    var itemTotalPrice = material.SalePrice * allocatedQuantity;

                    var cartItemVM = new CartItemVM
                    {
                        MaterialId = cartItem.MaterialId,
                        VariantId = cartItem.VariantId,
                        Quantity = allocatedQuantity,
                        ItemName = material.Name,
                        SalePrice = material.SalePrice,
                        ItemTotalPrice = itemTotalPrice,
                        ImageUrl = material.ImageUrl
                    };

                    // Xử lý biến thể (variant) nếu có
                    if (!string.IsNullOrEmpty(cartItem.VariantId))
                    {
                        var variant = await _variantService.FindAsync(Guid.Parse(cartItem.VariantId));
                        if (variant != null)
                        {
                            cartItemVM.SalePrice = variant.Price;
                            cartItemVM.ImageUrl = variant.VariantImageUrl;
                            cartItemVM.ItemTotalPrice = variant.Price * allocatedQuantity;
                            cartItemVM.ItemName += $" | test";
                        }
                    }

                    // Tìm hoặc tạo mới cửa hàng trong danh sách kết quả
                    var storeResult = result.FirstOrDefault(x => x.StoreId == store.Store.Id);
                    if (storeResult == null)
                    {
                        storeResult = new PreCheckOutItemCartModel
                        {
                            StoreId = store.Store.Id,
                            StoreName = store.Store.Name,
                            StoreItems = new List<CartItemVM>(),
                            TotalStoreAmount = 0
                        };
                        result.Add(storeResult);
                    }

                    // Thêm sản phẩm vào danh sách của cửa hàng
                    storeResult.StoreItems.Add(cartItemVM);
                    storeResult.TotalStoreAmount += cartItemVM.ItemTotalPrice;

                    // Cập nhật số lượng còn lại
                    remainingQuantity -= allocatedQuantity;

                    if (remainingQuantity == 0)
                        break; // Sản phẩm đã được phân bổ đủ
                }

                if (remainingQuantity > 0)
                {
                    throw new InvalidOperationException($"Không thể phân bổ đủ số lượng cho sản phẩm {cartItem.MaterialId}");
                }
            }
            return result;
        }


        #region CRUD
        public async Task AddAsync(StoreInventory inventory)
        {
            await _inventoryRepository.AddAsync(inventory);
        }

        public async Task AddRange(IEnumerable<StoreInventory> inventories)
        {
            await _inventoryRepository.AddRangce(inventories);
        }

        public async Task<bool> CheckExist(Expression<Func<StoreInventory, bool>> where)
        {
            return await _inventoryRepository.CheckExist(where);
        }

        public async Task<StoreInventory> FindAsync(Guid id)
        {
            return await _inventoryRepository.FindAsync(id);
        }

        public IQueryable<StoreInventory> Get(Expression<Func<StoreInventory, bool>> where)
        {
            return _inventoryRepository.Get(where);
        }

        public IQueryable<StoreInventory> Get(Expression<Func<StoreInventory, bool>> where, params Expression<Func<StoreInventory, object>>[] includes)
        {
            return _inventoryRepository.Get(where, includes);
        }

        public IQueryable<StoreInventory> Get(Expression<Func<StoreInventory, bool>> where, Func<IQueryable<StoreInventory>, IIncludableQueryable<StoreInventory, object>> include = null)
        {
            return _inventoryRepository.Get(where, include);
        }

        public IQueryable<StoreInventory> GetAll()
        {
            return _inventoryRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _inventoryRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(StoreInventory inventory)
        {
            _inventoryRepository.Update(inventory);
        }

        #endregion

    }
}
