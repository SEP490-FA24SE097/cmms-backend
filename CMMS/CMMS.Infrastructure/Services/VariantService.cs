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
    public interface IVariantService
    {
        Task<Variant> FindAsync(Guid id);
        IQueryable<Variant> GetAll();
        IQueryable<Variant> Get(Expression<Func<Variant, bool>> where);
        IQueryable<Variant> Get(Expression<Func<Variant, bool>> where, params Expression<Func<Variant, object>>[] includes);
        IQueryable<Variant> Get(Expression<Func<Variant, bool>> where, Func<IQueryable<Variant>, IIncludableQueryable<Variant, object>> include = null);
        Task AddAsync(Variant variant);
        Task AddRange(IEnumerable<Variant> variants);
        void Update(Variant variant);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Variant, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class VariantService : IVariantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVariantRepository _variantRepository;

        public VariantService(IUnitOfWork unitOfWork, IVariantRepository variantRepository)
        {
            _unitOfWork = unitOfWork;
            _variantRepository = variantRepository;
        }

        public async Task AddAsync(Variant variant)
        {
            await _variantRepository.AddAsync(variant);
        }

        public async Task AddRange(IEnumerable<Variant> variants)
        {
            await _variantRepository.AddRangce(variants);
        }

        public async Task<bool> CheckExist(Expression<Func<Variant, bool>> where)
        {
            return await _variantRepository.CheckExist(where);
        }

        public async Task<Variant> FindAsync(Guid id)
        {
            return await _variantRepository.FindAsync(id);
        }

        public IQueryable<Variant> Get(Expression<Func<Variant, bool>> where)
        {
            return _variantRepository.Get(where);
        }

        public IQueryable<Variant> Get(Expression<Func<Variant, bool>> where, params Expression<Func<Variant, object>>[] includes)
        {
            return _variantRepository.Get(where, includes);
        }

        public IQueryable<Variant> Get(Expression<Func<Variant, bool>> where, Func<IQueryable<Variant>, IIncludableQueryable<Variant, object>> include = null)
        {
            return _variantRepository.Get(where, include);
        }

        public IQueryable<Variant> GetAll()
        {
            return _variantRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _variantRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Variant variant)
        {
            _variantRepository.Update(variant);
        }
    }
}
