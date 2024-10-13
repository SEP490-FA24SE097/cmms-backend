using CMMS.Core.Entities;
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
    public interface IWarehouseService
    {
        Task<Warehouse> FindAsync(Guid id);
        IQueryable<Warehouse> GetAll();
        IQueryable<Warehouse> Get(Expression<Func<Warehouse, bool>> where);
        IQueryable<Warehouse> Get(Expression<Func<Warehouse, bool>> where, params Expression<Func<Warehouse, object>>[] includes);
        IQueryable<Warehouse> Get(Expression<Func<Warehouse, bool>> where, Func<IQueryable<Warehouse>, IIncludableQueryable<Warehouse, object>> include = null);
        Task AddAsync(Warehouse warehouse);
        Task AddRange(IEnumerable<Warehouse> warehouses);
        void Update(Warehouse warehouse);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Warehouse, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseService(IUnitOfWork unitOfWork, IWarehouseRepository warehouseRepository)
        {
            _unitOfWork = unitOfWork;
            _warehouseRepository = warehouseRepository;
        }

        public async Task AddAsync(Warehouse warehouse)
        {
            await _warehouseRepository.AddAsync(warehouse);
        }

        public async Task AddRange(IEnumerable<Warehouse> warehouses)
        {
            await _warehouseRepository.AddRangce(warehouses);
        }

        public async Task<bool> CheckExist(Expression<Func<Warehouse, bool>> where)
        {
            return await _warehouseRepository.CheckExist(where);
        }

        public async Task<Warehouse> FindAsync(Guid id)
        {
            return await _warehouseRepository.FindAsync(id);
        }

        public IQueryable<Warehouse> Get(Expression<Func<Warehouse, bool>> where)
        {
            return _warehouseRepository.Get(where);
        }

        public IQueryable<Warehouse> Get(Expression<Func<Warehouse, bool>> where, params Expression<Func<Warehouse, object>>[] includes)
        {
            return _warehouseRepository.Get(where, includes);
        }

        public IQueryable<Warehouse> Get(Expression<Func<Warehouse, bool>> where, Func<IQueryable<Warehouse>, IIncludableQueryable<Warehouse, object>> include = null)
        {
            return _warehouseRepository.Get(where, include);
        }

        public IQueryable<Warehouse> GetAll()
        {
            return _warehouseRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _warehouseRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Warehouse warehouse)
        {
            _warehouseRepository.Update(warehouse);
        }
    }
}
