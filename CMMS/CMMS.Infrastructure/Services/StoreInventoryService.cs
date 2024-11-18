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
    }

    public class StoreInventoryService : IStoreInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreInventoryRepository _inventoryRepository;
        private readonly IMapper _mapper;

        public StoreInventoryService(IUnitOfWork unitOfWork, IStoreInventoryRepository inventoryRepository,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _inventoryRepository = inventoryRepository;
            _mapper = mapper;

        }
        public async Task<StoreInventory> GetItemInStoreAsync(AddItemModel itemModel)
        {
            StoreInventory storeInventory = null;
            if (itemModel.VariantId != null)
            {
                var materialId = Guid.Parse(itemModel.MaterialId);
                var variantId = Guid.Parse(itemModel.VariantId);
                storeInventory = await _inventoryRepository.Get(x =>
              x.StoreId.Equals(itemModel.StoreId)
              && x.MaterialId.Equals(materialId)
              && x.VariantId.Equals(variantId)).FirstOrDefaultAsync();
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
            if (storeInventory != null)
            {
                var availableQuantity = storeInventory.TotalQuantity - storeInventory.InOrderQuantity;
                return cartItem.Quantity > availableQuantity;
            }
            return false;
        }
        public async Task<bool> UpdateStoreInventoryAsync(CartItem cartItem, int invoiceStatus)
        {
            var item = _mapper.Map<AddItemModel>(cartItem);
            var storeInventory = await GetItemInStoreAsync(item);
            if (storeInventory != null)
            {
                switch(invoiceStatus)
                {
                    case (int)InvoiceStatus.Pending :
                        storeInventory.InOrderQuantity += cartItem.Quantity;
                        break;
                    case (int)InvoiceStatus.Done:
                        storeInventory.TotalQuantity -= cartItem.Quantity;
                        storeInventory.InOrderQuantity -= cartItem.Quantity;
                        break;
                }
                _inventoryRepository.Update(storeInventory);
                var result = await _unitOfWork.SaveChangeAsync();
                if (result) return true;
            }
            return false;
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
