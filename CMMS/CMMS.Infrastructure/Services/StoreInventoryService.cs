using CMMS.Core.Entities;
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
        decimal GetAvailableQuantityInAllStore(CartItemWithoutStoreId cartItem);
        Task<List<PreCheckOutItemCartModel>> DistributeItemsToStores(CartItemRequest cartItems, List<StoreDistance> listStoreByDistance);
        Task<decimal?> GetConversionRate(string materialId, string? variantId);
    }

    public class StoreInventoryService : IStoreInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreInventoryRepository _inventoryRepository;
        private readonly IMapper _mapper;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;

        public StoreInventoryService(IUnitOfWork unitOfWork, IStoreInventoryRepository
            inventoryRepository, IVariantService variantService,
            IMapper mapper, IMaterialService materialService, IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _unitOfWork = unitOfWork;
            _inventoryRepository = inventoryRepository;
            _mapper = mapper;
            _variantService = variantService;
            _materialService = materialService;
            _materialVariantAttributeService = materialVariantAttributeService;
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
        public async Task<decimal?> GetConversionRate(string materialId, string? variantId)
        {
            if (variantId == null)
                return null;
            else
            {
                var variant = await _variantService.Get(x => x.Id == Guid.Parse(variantId)).Include(x => x.ConversionUnit).FirstOrDefaultAsync();

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
            var conversionRate = await GetConversionRate(cartItem.MaterialId, cartItem.VariantId);
            if (storeInventory != null)
            {
                var availableQuantity = storeInventory.TotalQuantity - (storeInventory.InOrderQuantity ?? 0);
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
            var conversionRate = await GetConversionRate(cartItem.MaterialId, cartItem.VariantId);
            var orderQuantity = conversionRate == null ? cartItem.Quantity : cartItem.Quantity * conversionRate;
            if (storeInventory != null)
            {
                switch (invoiceStatus)
                {
                    case (int)InvoiceStatus.Pending:
                        storeInventory.InOrderQuantity = (storeInventory.InOrderQuantity ?? 0) + orderQuantity;
                        break;
                    case (int)InvoiceStatus.Done:

                        storeInventory.TotalQuantity -= (decimal)orderQuantity;
                        storeInventory.InOrderQuantity = (storeInventory.InOrderQuantity ?? 0) - orderQuantity;
                        storeInventory.SoldQuantity += cartItem.Quantity;
                        break;
                    case (int)InvoiceStatus.DoneInStore:
                        storeInventory.TotalQuantity -= (decimal)orderQuantity;
                        break;
                    case (int)InvoiceStatus.Cancel:
                    case (int)InvoiceStatus.Refund:
                        storeInventory.TotalQuantity -= (decimal)orderQuantity;
                        storeInventory.SoldQuantity -= cartItem.Quantity;
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
            var conversionRate = await GetConversionRate(cartItem.MaterialId, cartItem.VariantId);
            if (storeInventory != null)
            {
                //var conversionRate = await GetConversionRate(storeInventory.MaterialId, storeInventory.VariantId);
                if (storeInventory != null)
                {
                    // var availableQuantity = storeInventory.TotalQuantity - storeInventory.InOrderQuantity;
                    var availableQuantity = conversionRate == null ? storeInventory.TotalQuantity - (storeInventory.InOrderQuantity ?? 0) : (storeInventory.TotalQuantity - (storeInventory.InOrderQuantity ?? 0)) / conversionRate;
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

                    // SalePrice chỗ này sẽ bằng giá tiền sau khi giảm giá.
                    var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                    var finalPrice = await _materialService.GetAfterDiscountPrice(material.Id.ToString(), null);
                    var itemTotalPrice = material.SalePrice * allocatedQuantity;

                    var cartItemVM = new CartItemVM
                    {
                        MaterialId = cartItem.MaterialId,
                        VariantId = cartItem.VariantId,
                        Quantity = allocatedQuantity,
                        ItemName = material.Name,
                        SalePrice = finalPrice,
                        BeforeDiscountPrice = material.SalePrice,
                        ItemTotalPrice = itemTotalPrice,
                        ImageUrl = material.ImageUrl,
                    };

                    // check item was discount or not
                    if (finalPrice != material.SalePrice) cartItemVM.isDiscount = true;

                    // Xử lý biến thể (variant) nếu có
                    if (!string.IsNullOrEmpty(cartItem.VariantId))
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                        if (variant != null)
                        {
                            finalPrice = await _materialService.GetAfterDiscountPrice(material.Id.ToString(), variant.Id.ToString());
                            if (variant.MaterialVariantAttributes != null && variant.MaterialVariantAttributes.Count > 0)
                            {
                                var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                                var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                                cartItemVM.ItemName = $"{variant.SKU} {attributesString}";
                            }
                            else
                            {
                                cartItemVM.ItemName = $"{variant.SKU}";
                            }
                            cartItemVM.BeforeDiscountPrice = variant.Price;
                            cartItemVM.SalePrice = finalPrice;
                            cartItemVM.ImageUrl = variant.VariantImageUrl;
                            cartItemVM.ItemTotalPrice = variant.Price * cartItemVM.Quantity;
                            // check item was discount or not
                            if (finalPrice != variant.Price) cartItemVM.isDiscount = true;
                        }
                    }

                    var storeInventoryItem = _mapper.Map<CartItem>(cartItemVM);
                    storeInventoryItem.StoreId = store.Store.Id;
                    cartItemVM.InStock = await GetAvailableQuantityInStore(storeInventoryItem);


                    // Tìm hoặc tạo mới cửa hàng trong danh sách kết quả
                    var storeResult = result.FirstOrDefault(x => x.StoreId == store.Store.Id);
                    if (storeResult == null)
                    {
                        storeResult = new PreCheckOutItemCartModel
                        {
                            StoreId = store.Store.Id,
                            StoreName = store.Store.Name,
                            StoreItems = new List<CartItemVM>(),
                            TotalStoreAmount = 0,
                            ShippingDistance = store.Distance
                        };
                        result.Add(storeResult);
                    }

                    // Thêm sản phẩm vào danh sách của cửa hàng
                    storeResult.StoreItems.Add(cartItemVM);
                    //storeResult.TotalStoreAmount += cartItemVM.ItemTotalPrice;
                    storeResult.TotalStoreAmount += (cartItemVM.BeforeDiscountPrice * cartItemVM.Quantity);

                    // Cập nhật số lượng còn lại
                    remainingQuantity -= allocatedQuantity;

                    if (remainingQuantity == 0)
                        break; // Sản phẩm đã được phân bổ đủ
                }

                if (remainingQuantity > 0)
                {
                    
                }
            }
            return result;
        }

        public decimal GetAvailableQuantityInAllStore(CartItemWithoutStoreId cartItem)
        {
            var materialId = Guid.Parse(cartItem.MaterialId);
            Guid? variantId = cartItem.VariantId != null ? Guid.Parse(cartItem.VariantId) : null;
            if (variantId != null)
            {
                var variant = _variantService.Get(x => x.Id == variantId).Include(x => x.ConversionUnit).FirstOrDefault();
                if (variant != null)
                {
                    if (variant.ConversionUnitId != null)
                    {
                        var rootVariantItems = _inventoryRepository
                            .Get(x => x.VariantId == variant.AttributeVariantId).Include(x => x.Store).ToList();
                        if (rootVariantItems.Count > 0)
                        {
                            var result = rootVariantItems.Select(x => new
                            {
                                storeId = x.StoreId,
                                storeName = x.Store.Name,
                                quantity = (x.TotalQuantity - (x.InOrderQuantity ?? 0)) / variant.ConversionUnit.ConversionRate
                            }).Sum(x => x.quantity);
                            return (decimal)result;
                        }
                    }
                }
            }

            var availableQuantity = _inventoryRepository.Get(x =>
           x.MaterialId == materialId &&
           x.VariantId == variantId && x.TotalQuantity > 0).Include(x => x.Store).Select(x => new
           {
               storeId = x.StoreId,
               storeName = x.Store.Name,
               quantity = x.TotalQuantity - (x.InOrderQuantity ?? 0)
           }).Sum(_ => _.quantity);



            return (decimal)availableQuantity;
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
