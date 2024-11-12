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
    public interface IGoodsDeliveryNoteService
    {
        Task<GoodsDeliveryNote> FindAsync(Guid id);
        IQueryable<GoodsDeliveryNote> GetAll();
        IQueryable<GoodsDeliveryNote> Get(Expression<Func<GoodsDeliveryNote, bool>> where);
        IQueryable<GoodsDeliveryNote> Get(Expression<Func<GoodsDeliveryNote, bool>> where, params Expression<Func<GoodsDeliveryNote, object>>[] includes);
        IQueryable<GoodsDeliveryNote> Get(Expression<Func<GoodsDeliveryNote, bool>> where, Func<IQueryable<GoodsDeliveryNote>, IIncludableQueryable<GoodsDeliveryNote, object>> include = null);
        Task AddAsync(GoodsDeliveryNote goodsDeliveryNote);
        Task AddRangeAsync(IEnumerable<GoodsDeliveryNote> goodsDeliveryNotes);
        void Update(GoodsDeliveryNote goodsDeliveryNote);
        Task<bool> RemoveAsync(Guid id);
        Task<bool> CheckExistAsync(Expression<Func<GoodsDeliveryNote, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class GoodsDeliveryNoteService : IGoodsDeliveryNoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoodsDeliveryNoteRepository _goodsDeliveryNoteRepository;

        public GoodsDeliveryNoteService(IUnitOfWork unitOfWork, IGoodsDeliveryNoteRepository goodsDeliveryNoteRepository)
        {
            _unitOfWork = unitOfWork;
            _goodsDeliveryNoteRepository = goodsDeliveryNoteRepository;
        }

        public async Task AddAsync(GoodsDeliveryNote goodsDeliveryNote)
        {
            await _goodsDeliveryNoteRepository.AddAsync(goodsDeliveryNote);
        }

        public async Task AddRangeAsync(IEnumerable<GoodsDeliveryNote> goodsDeliveryNotes)
        {
            await _goodsDeliveryNoteRepository.AddRangce(goodsDeliveryNotes);
        }

        public async Task<bool> CheckExistAsync(Expression<Func<GoodsDeliveryNote, bool>> where)
        {
            return await _goodsDeliveryNoteRepository.CheckExist(where);
        }

        public async Task<GoodsDeliveryNote> FindAsync(Guid id)
        {
            return await _goodsDeliveryNoteRepository.FindAsync(id);
        }

        public IQueryable<GoodsDeliveryNote> Get(Expression<Func<GoodsDeliveryNote, bool>> where)
        {
            return _goodsDeliveryNoteRepository.Get(where);
        }

        public IQueryable<GoodsDeliveryNote> Get(Expression<Func<GoodsDeliveryNote, bool>> where, params Expression<Func<GoodsDeliveryNote, object>>[] includes)
        {
            return _goodsDeliveryNoteRepository.Get(where, includes);
        }

        public IQueryable<GoodsDeliveryNote> Get(Expression<Func<GoodsDeliveryNote, bool>> where, Func<IQueryable<GoodsDeliveryNote>, IIncludableQueryable<GoodsDeliveryNote, object>> include = null)
        {
            return _goodsDeliveryNoteRepository.Get(where, include);
        }

        public IQueryable<GoodsDeliveryNote> GetAll()
        {
            return _goodsDeliveryNoteRepository.GetAll();
        }

        public async Task<bool> RemoveAsync(Guid id)
        {
            return await _goodsDeliveryNoteRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(GoodsDeliveryNote goodsDeliveryNote)
        {
            _goodsDeliveryNoteRepository.Update(goodsDeliveryNote);
        }
    }
}
