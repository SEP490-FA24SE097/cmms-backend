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
    public interface IGoodsNoteDetailService
    {
        Task<GoodsNoteDetail> FindAsync(Guid id);
        IQueryable<GoodsNoteDetail> GetAll();
        IQueryable<GoodsNoteDetail> Get(Expression<Func<GoodsNoteDetail, bool>> where);
        IQueryable<GoodsNoteDetail> Get(Expression<Func<GoodsNoteDetail, bool>> where, params Expression<Func<GoodsNoteDetail, object>>[] includes);
        IQueryable<GoodsNoteDetail> Get(Expression<Func<GoodsNoteDetail, bool>> where, Func<IQueryable<GoodsNoteDetail>, IIncludableQueryable<GoodsNoteDetail, object>> include = null);
        Task AddAsync(GoodsNoteDetail goodsDeliveryNoteDetail);
        Task AddRangeAsync(IEnumerable<GoodsNoteDetail> goodsDeliveryNoteDetails);
        void Update(GoodsNoteDetail goodsDeliveryNoteDetail);
        Task<bool> RemoveAsync(Guid id);
        Task<bool> CheckExistAsync(Expression<Func<GoodsNoteDetail, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class GoodsNoteDetailService : IGoodsNoteDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoodsNoteDetailRepository _goodsDeliveryNoteDetailRepository;

        public GoodsNoteDetailService(IUnitOfWork unitOfWork, IGoodsNoteDetailRepository goodsDeliveryNoteDetailRepository)
        {
            _unitOfWork = unitOfWork;
            _goodsDeliveryNoteDetailRepository = goodsDeliveryNoteDetailRepository;
        }

        public async Task AddAsync(GoodsNoteDetail goodsDeliveryNoteDetail)
        {
            await _goodsDeliveryNoteDetailRepository.AddAsync(goodsDeliveryNoteDetail);
        }

        public async Task AddRangeAsync(IEnumerable<GoodsNoteDetail> goodsDeliveryNoteDetails)
        {
            await _goodsDeliveryNoteDetailRepository.AddRangce(goodsDeliveryNoteDetails);
        }

        public async Task<bool> CheckExistAsync(Expression<Func<GoodsNoteDetail, bool>> where)
        {
            return await _goodsDeliveryNoteDetailRepository.CheckExist(where);
        }

        public async Task<GoodsNoteDetail> FindAsync(Guid id)
        {
            return await _goodsDeliveryNoteDetailRepository.FindAsync(id);
        }

        public IQueryable<GoodsNoteDetail> Get(Expression<Func<GoodsNoteDetail, bool>> where)
        {
            return _goodsDeliveryNoteDetailRepository.Get(where);
        }

        public IQueryable<GoodsNoteDetail> Get(Expression<Func<GoodsNoteDetail, bool>> where, params Expression<Func<GoodsNoteDetail, object>>[] includes)
        {
            return _goodsDeliveryNoteDetailRepository.Get(where, includes);
        }

        public IQueryable<GoodsNoteDetail> Get(Expression<Func<GoodsNoteDetail, bool>> where, Func<IQueryable<GoodsNoteDetail>, IIncludableQueryable<GoodsNoteDetail, object>> include = null)
        {
            return _goodsDeliveryNoteDetailRepository.Get(where, include);
        }

        public IQueryable<GoodsNoteDetail> GetAll()
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

        public void Update(GoodsNoteDetail goodsDeliveryNoteDetail)
        {
            _goodsDeliveryNoteDetailRepository.Update(goodsDeliveryNoteDetail);
        }
    }
}
