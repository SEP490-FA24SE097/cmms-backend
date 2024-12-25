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
    public interface IGoodsNoteService
    {
        Task<GoodsNote> FindAsync(Guid id);
        IQueryable<GoodsNote> GetAll();
        IQueryable<GoodsNote> Get(Expression<Func<GoodsNote, bool>> where);
        IQueryable<GoodsNote> Get(Expression<Func<GoodsNote, bool>> where, params Expression<Func<GoodsNote, object>>[] includes);
        IQueryable<GoodsNote> Get(Expression<Func<GoodsNote, bool>> where, Func<IQueryable<GoodsNote>, IIncludableQueryable<GoodsNote, object>> include = null);
        Task AddAsync(GoodsNote goodsDeliveryNote);
        Task AddRangeAsync(IEnumerable<GoodsNote> goodsDeliveryNotes);
        void Update(GoodsNote goodsDeliveryNote);
        Task<bool> RemoveAsync(Guid id);
        Task<bool> CheckExistAsync(Expression<Func<GoodsNote, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class GoodsNoteService : IGoodsNoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoodsNoteRepository _goodsDeliveryNoteRepository;

        public GoodsNoteService(IUnitOfWork unitOfWork, IGoodsNoteRepository goodsDeliveryNoteRepository)
        {
            _unitOfWork = unitOfWork;
            _goodsDeliveryNoteRepository = goodsDeliveryNoteRepository;
        }

        public async Task AddAsync(GoodsNote goodsDeliveryNote)
        {
            await _goodsDeliveryNoteRepository.AddAsync(goodsDeliveryNote);
        }

        public async Task AddRangeAsync(IEnumerable<GoodsNote> goodsDeliveryNotes)
        {
            await _goodsDeliveryNoteRepository.AddRangce(goodsDeliveryNotes);
        }

        public async Task<bool> CheckExistAsync(Expression<Func<GoodsNote, bool>> where)
        {
            return await _goodsDeliveryNoteRepository.CheckExist(where);
        }

        public async Task<GoodsNote> FindAsync(Guid id)
        {
            return await _goodsDeliveryNoteRepository.FindAsync(id);
        }

        public IQueryable<GoodsNote> Get(Expression<Func<GoodsNote, bool>> where)
        {
            return _goodsDeliveryNoteRepository.Get(where);
        }

        public IQueryable<GoodsNote> Get(Expression<Func<GoodsNote, bool>> where, params Expression<Func<GoodsNote, object>>[] includes)
        {
            return _goodsDeliveryNoteRepository.Get(where, includes);
        }

        public IQueryable<GoodsNote> Get(Expression<Func<GoodsNote, bool>> where, Func<IQueryable<GoodsNote>, IIncludableQueryable<GoodsNote, object>> include = null)
        {
            return _goodsDeliveryNoteRepository.Get(where, include);
        }

        public IQueryable<GoodsNote> GetAll()
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

        public void Update(GoodsNote goodsDeliveryNote)
        {
            _goodsDeliveryNoteRepository.Update(goodsDeliveryNote);
        }
    }
}
