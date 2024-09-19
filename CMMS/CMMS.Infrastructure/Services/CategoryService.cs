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
    public interface ICategoryService
    {
        Task<Category> FindAsync(Guid id);
        IQueryable<Category> GetAll();
        IQueryable<Category> Get(Expression<Func<Category, bool>> where);
        IQueryable<Category> Get(Expression<Func<Category, bool>> where, params Expression<Func<Category, object>>[] includes);
        IQueryable<Category> Get(Expression<Func<Category, bool>> where, Func<IQueryable<Category>, IIncludableQueryable<Category, object>> include = null);
        Task AddAsync(Category category);
        Task AddRange(IEnumerable<Category> categories);
        void Update(Category category);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Category, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class CategoryService : ICategoryService
    {
        private IUnitOfWork _unitOfWork;
        private ICategoryRepository _CategoryRepository;
        public CategoryService(IUnitOfWork unitOfWork, ICategoryRepository CategoryRepository)
        {
            _unitOfWork = unitOfWork;
            _CategoryRepository = CategoryRepository;
        }

        public async Task AddAsync(Category category)
        {
            await _CategoryRepository.AddAsync(category);
        }

      

        public async Task AddRange(IEnumerable<Category> categories)
        {
            await _CategoryRepository.AddRangce(categories);
        }

        public async Task<bool> CheckExist(Expression<Func<Category, bool>> where)
        {
            return await _CategoryRepository.CheckExist(where);
        }

        public async Task<Category> FindAsync(Guid id)
        {
            return await _CategoryRepository.FindAsync(id);
        }

        public IQueryable<Category> Get(Expression<Func<Category, bool>> where)
        {
            return _CategoryRepository.Get(where);
        }

        public IQueryable<Category> Get(Expression<Func<Category, bool>> where, params Expression<Func<Category, object>>[] includes)
        {
            return _CategoryRepository.Get(where, includes);
        }

        public IQueryable<Category> Get(Expression<Func<Category, bool>> where, Func<IQueryable<Category>, IIncludableQueryable<Category, object>> include = null)
        {
            return _CategoryRepository.Get(where, include);
        }

        public IQueryable<Category> GetAll()
        {
            return _CategoryRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _CategoryRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Category category)
        {
            _CategoryRepository.Update(category);
        }
    }
}
