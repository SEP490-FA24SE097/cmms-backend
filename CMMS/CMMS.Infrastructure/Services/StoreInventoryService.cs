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

namespace CMMS.Infrastructure.Services
{
    public interface IStoreInventoryService
    {
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
    }

    public class StoreInventoryService : IStoreInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreInventoryRepository _inventoryRepository;

        public StoreInventoryService(IUnitOfWork unitOfWork, IStoreInventoryRepository inventoryRepository)
        {
            _unitOfWork = unitOfWork;
            _inventoryRepository = inventoryRepository;
        }

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
    }
}
