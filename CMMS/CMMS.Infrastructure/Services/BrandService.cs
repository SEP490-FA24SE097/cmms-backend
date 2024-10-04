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
    public interface IBrandService
    {
        Task<Brand> FindAsync(Guid id);
        IQueryable<Brand> GetAll();
        IQueryable<Brand> Get(Expression<Func<Brand, bool>> where);
        IQueryable<Brand> Get(Expression<Func<Brand, bool>> where, params Expression<Func<Brand, object>>[] includes);
        IQueryable<Brand> Get(Expression<Func<Brand, bool>> where, Func<IQueryable<Brand>, IIncludableQueryable<Brand, object>> include = null);
        Task AddAsync(Brand brand);
        Task AddRange(IEnumerable<Brand> brands);
        void Update(Brand brand);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Brand, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class BrandService : IBrandService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBrandRepository _brandRepository;

        public BrandService(IUnitOfWork unitOfWork, IBrandRepository brandRepository)
        {
            _unitOfWork = unitOfWork;
            _brandRepository = brandRepository;
        }

        public async Task AddAsync(Brand brand)
        {
            await _brandRepository.AddAsync(brand);
        }

        public async Task AddRange(IEnumerable<Brand> brands)
        {
            await _brandRepository.AddRangce(brands);
        }

        public async Task<bool> CheckExist(Expression<Func<Brand, bool>> where)
        {
            return await _brandRepository.CheckExist(where);
        }

        public async Task<Brand> FindAsync(Guid id)
        {
            return await _brandRepository.FindAsync(id);
        }

        public IQueryable<Brand> Get(Expression<Func<Brand, bool>> where)
        {
            return _brandRepository.Get(where);
        }

        public IQueryable<Brand> Get(Expression<Func<Brand, bool>> where, params Expression<Func<Brand, object>>[] includes)
        {
            return _brandRepository.Get(where, includes);
        }

        public IQueryable<Brand> Get(Expression<Func<Brand, bool>> where, Func<IQueryable<Brand>, IIncludableQueryable<Brand, object>> include = null)
        {
            return _brandRepository.Get(where, include);
        }

        public IQueryable<Brand> GetAll()
        {
            return _brandRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _brandRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Brand brand)
        {
            _brandRepository.Update(brand);
        }
    }
}
