using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CMMS.Infrastructure.Repositories;

namespace CMMS.Infrastructure.Services
{
    public interface ISupplierService
    {
        Task<Supplier> FindAsync(Guid id);
        IQueryable<Supplier> GetAll();
        IQueryable<Supplier> Get(Expression<Func<Supplier, bool>> where);
        IQueryable<Supplier> Get(Expression<Func<Supplier, bool>> where, params Expression<Func<Supplier, object>>[] includes);
        IQueryable<Supplier> Get(Expression<Func<Supplier, bool>> where, Func<IQueryable<Supplier>, IIncludableQueryable<Supplier, object>> include = null);
        Task AddAsync(Supplier supplier);
        Task AddRange(IEnumerable<Supplier> suppliers);
        void Update(Supplier supplier);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Supplier, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class SupplierService : ISupplierService
    {
        private IUnitOfWork _unitOfWork;
        private ISupplierRepository _supplierRepository;
        public SupplierService(IUnitOfWork unitOfWork, ISupplierRepository supplierRepository)
        {
            _unitOfWork = unitOfWork;
            _supplierRepository = supplierRepository;
        }

        public async Task AddAsync(Supplier supplier)
        {
            await _supplierRepository.AddAsync(supplier);
        }



        public async Task AddRange(IEnumerable<Supplier> suppliers)
        {
            await _supplierRepository.AddRangce(suppliers);
        }

        public async Task<bool> CheckExist(Expression<Func<Supplier, bool>> where)
        {
            return await _supplierRepository.CheckExist(where);
        }

        public async Task<Supplier> FindAsync(Guid id)
        {
            return await _supplierRepository.FindAsync(id);
        }

        public IQueryable<Supplier> Get(Expression<Func<Supplier, bool>> where)
        {
            return _supplierRepository.Get(where);
        }

        public IQueryable<Supplier> Get(Expression<Func<Supplier, bool>> where, params Expression<Func<Supplier, object>>[] includes)
        {
            return _supplierRepository.Get(where, includes);
        }

        public IQueryable<Supplier> Get(Expression<Func<Supplier, bool>> where, Func<IQueryable<Supplier>, IIncludableQueryable<Supplier, object>> include = null)
        {
            return _supplierRepository.Get(where, include);
        }

        public IQueryable<Supplier> GetAll()
        {
            return _supplierRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _supplierRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Supplier supplier)
        {
            _supplierRepository.Update(supplier);
        }
    }
}
