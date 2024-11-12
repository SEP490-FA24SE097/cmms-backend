using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IGoodsDeliveryNoteDetailService
    {
        Task<GoodsDeliveryNoteDetail> FindAsync(Guid id);
        IQueryable<GoodsDeliveryNoteDetail> GetAll();
        IQueryable<GoodsDeliveryNoteDetail> Get(Expression<Func<GoodsDeliveryNoteDetail, bool>> where);
        IQueryable<GoodsDeliveryNoteDetail> Get(Expression<Func<GoodsDeliveryNoteDetail, bool>> where, params Expression<Func<GoodsDeliveryNoteDetail, object>>[] includes);
        IQueryable<GoodsDeliveryNoteDetail> Get(Expression<Func<GoodsDeliveryNoteDetail, bool>> where, Func<IQueryable<GoodsDeliveryNoteDetail>, IIncludableQueryable<GoodsDeliveryNoteDetail, object>> include = null);
        Task AddAsync(GoodsDeliveryNoteDetail goodsDeliveryNoteDetail);
        Task AddRangeAsync(IEnumerable<GoodsDeliveryNoteDetail> goodsDeliveryNoteDetails);
        void Update(GoodsDeliveryNoteDetail goodsDeliveryNoteDetail);
        Task<bool> RemoveAsync(Guid id);
        Task<bool> CheckExistAsync(Expression<Func<GoodsDeliveryNoteDetail, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class GoodsDeliveryNoteDetailService : IGoodsDeliveryNoteDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoodsDeliveryNoteDetailRepository _goodsDeliveryNoteDetailRepository;

        public GoodsDeliveryNoteDetailService(IUnitOfWork unitOfWork, IGoodsDeliveryNoteDetailRepository goodsDeliveryNoteDetailRepository)
        {
            _unitOfWork = unitOfWork;
            _goodsDeliveryNoteDetailRepository = goodsDeliveryNoteDetailRepository;
        }

        public async Task AddAsync(GoodsDeliveryNoteDetail goodsDeliveryNoteDetail)
        {
            await _goodsDeliveryNoteDetailRepository.AddAsync(goodsDeliveryNoteDetail);
        }

        public async Task AddRangeAsync(IEnumerable<GoodsDeliveryNoteDetail> goodsDeliveryNoteDetails)
        {
            await _goodsDeliveryNoteDetailRepository.AddRangce(goodsDeliveryNoteDetails);
        }

        public async Task<bool> CheckExistAsync(Expression<Func<GoodsDeliveryNoteDetail, bool>> where)
        {
            return await _goodsDeliveryNoteDetailRepository.CheckExist(where);
        }

        public async Task<GoodsDeliveryNoteDetail> FindAsync(Guid id)
        {
            return await _goodsDeliveryNoteDetailRepository.FindAsync(id);
        }

        public IQueryable<GoodsDeliveryNoteDetail> Get(Expression<Func<GoodsDeliveryNoteDetail, bool>> where)
        {
            return _goodsDeliveryNoteDetailRepository.Get(where);
        }

        public IQueryable<GoodsDeliveryNoteDetail> Get(Expression<Func<GoodsDeliveryNoteDetail, bool>> where, params Expression<Func<GoodsDeliveryNoteDetail, object>>[] includes)
        {
            return _goodsDeliveryNoteDetailRepository.Get(where, includes);
        }

        public IQueryable<GoodsDeliveryNoteDetail> Get(Expression<Func<GoodsDeliveryNoteDetail, bool>> where, Func<IQueryable<GoodsDeliveryNoteDetail>, IIncludableQueryable<GoodsDeliveryNoteDetail, object>> include = null)
        {
            return _goodsDeliveryNoteDetailRepository.Get(where, include);
        }

        public IQueryable<GoodsDeliveryNoteDetail> GetAll()
        {
            return _goodsDeliveryNoteDetailRepository.GetAll();
        }

        public async Task<bool> RemoveAsync(Guid id)
        {
            return await _goodsDeliveryNoteDetailRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(GoodsDeliveryNoteDetail goodsDeliveryNoteDetail)
        {
            _goodsDeliveryNoteDetailRepository.Update(goodsDeliveryNoteDetail);
        }
    }
}
