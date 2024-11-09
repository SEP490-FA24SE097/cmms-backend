using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IShippingDetailService
    {
        #region CURD
        Task<ShippingDetail> FindAsync(string id);
        IQueryable<ShippingDetail> GetAll();
        IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where);
        IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, params Expression<Func<ShippingDetail, object>>[] includes);
        IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, Func<IQueryable<ShippingDetail>, IIncludableQueryable<ShippingDetail, object>> include = null);
        Task AddAsync(ShippingDetail ShippingDetail);
        Task AddRange(IEnumerable<ShippingDetail> ShippingDetails);
        void Update(ShippingDetail ShippingDetail);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<ShippingDetail, bool>> where);
        Task<bool> SaveChangeAsync();
        #endregion
    }
    public class ShippingDetailService : IShippingDetailService
    {
        private IShippingDetailRepository _shippingDetailRepository;
        private IUnitOfWork _unitOfWork;

        public ShippingDetailService(IShippingDetailRepository shippingDetailRepository,
            IUnitOfWork unitOfWork)
        {
            _shippingDetailRepository = shippingDetailRepository;
            _unitOfWork = unitOfWork;
        }

        #region CURD 
        public async Task AddAsync(ShippingDetail ShippingDetail)
        {
            await _shippingDetailRepository.AddAsync(ShippingDetail);
        }

        public async Task AddRange(IEnumerable<ShippingDetail> ShippingDetails)
        {
            await _shippingDetailRepository.AddRangce(ShippingDetails);
        }

        public Task<bool> CheckExist(Expression<Func<ShippingDetail, bool>> where)
        {
            return _shippingDetailRepository.CheckExist(where);
        }

        public Task<ShippingDetail> FindAsync(string id)
        {
            return _shippingDetailRepository.FindAsync(id);
        }

        public IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where)
        {
            return _shippingDetailRepository.Get(where);
        }

        public IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, params Expression<Func<ShippingDetail, object>>[] includes)
        {
            return _shippingDetailRepository.Get(where, includes);
        }

        public IQueryable<ShippingDetail> Get(Expression<Func<ShippingDetail, bool>> where, Func<IQueryable<ShippingDetail>, IIncludableQueryable<ShippingDetail, object>> include = null)
        {
            return _shippingDetailRepository.Get(where, include);
        }

        public IQueryable<ShippingDetail> GetAll()
        {
            return _shippingDetailRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _shippingDetailRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(ShippingDetail ShippingDetail)
        {
            _shippingDetailRepository.Update(ShippingDetail);
        }
        #endregion
    }
}
