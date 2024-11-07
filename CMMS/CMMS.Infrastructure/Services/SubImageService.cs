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
    public interface ISubImageService
    {
        Task<SubImage> FindAsync(Guid id);
        IQueryable<SubImage> GetAll();
        IQueryable<SubImage> Get(Expression<Func<SubImage, bool>> where);
        IQueryable<SubImage> Get(Expression<Func<SubImage, bool>> where, params Expression<Func<SubImage, object>>[] includes);
        IQueryable<SubImage> Get(Expression<Func<SubImage, bool>> where, Func<IQueryable<SubImage>, IIncludableQueryable<SubImage, object>> include = null);
        Task AddAsync(SubImage subImage);
        Task AddRange(IEnumerable<SubImage> subImages);
        void Update(SubImage subImage);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<SubImage, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class SubImageService : ISubImageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISubImageRepository _subImageRepository;

        public SubImageService(IUnitOfWork unitOfWork, ISubImageRepository subImageRepository)
        {
            _unitOfWork = unitOfWork;
            _subImageRepository = subImageRepository;
        }

        public async Task AddAsync(SubImage subImage)
        {
            await _subImageRepository.AddAsync(subImage);
        }

        public async Task AddRange(IEnumerable<SubImage> subImages)
        {
            await _subImageRepository.AddRangce(subImages);
        }

        public async Task<bool> CheckExist(Expression<Func<SubImage, bool>> where)
        {
            return await _subImageRepository.CheckExist(where);
        }

        public async Task<SubImage> FindAsync(Guid id)
        {
            return await _subImageRepository.FindAsync(id);
        }

        public IQueryable<SubImage> Get(Expression<Func<SubImage, bool>> where)
        {
            return _subImageRepository.Get(where);
        }

        public IQueryable<SubImage> Get(Expression<Func<SubImage, bool>> where, params Expression<Func<SubImage, object>>[] includes)
        {
            return _subImageRepository.Get(where, includes);
        }

        public IQueryable<SubImage> Get(Expression<Func<SubImage, bool>> where, Func<IQueryable<SubImage>, IIncludableQueryable<SubImage, object>> include = null)
        {
            return _subImageRepository.Get(where, include);
        }

        public IQueryable<SubImage> GetAll()
        {
            return _subImageRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _subImageRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(SubImage subImage)
        {
            _subImageRepository.Update(subImage);
        }
    }
}
