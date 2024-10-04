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
    public interface IMaterialVariantAttributeService
    {
        Task<MaterialVariantAttribute> FindAsync(Guid id);
        IQueryable<MaterialVariantAttribute> GetAll();
        IQueryable<MaterialVariantAttribute> Get(Expression<Func<MaterialVariantAttribute, bool>> where);
        IQueryable<MaterialVariantAttribute> Get(Expression<Func<MaterialVariantAttribute, bool>> where, params Expression<Func<MaterialVariantAttribute, object>>[] includes);
        IQueryable<MaterialVariantAttribute> Get(Expression<Func<MaterialVariantAttribute, bool>> where, Func<IQueryable<MaterialVariantAttribute>, IIncludableQueryable<MaterialVariantAttribute, object>> include = null);
        Task AddAsync(MaterialVariantAttribute materialVariantAttribute);
        Task AddRange(IEnumerable<MaterialVariantAttribute> materialVariantAttributes);
        void Update(MaterialVariantAttribute materialVariantAttribute);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<MaterialVariantAttribute, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class MaterialVariantAttributeService : IMaterialVariantAttributeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMaterialVariantAttributeRepository _materialVariantAttributeRepository;

        public MaterialVariantAttributeService(IUnitOfWork unitOfWork, IMaterialVariantAttributeRepository materialVariantAttributeRepository)
        {
            _unitOfWork = unitOfWork;
            _materialVariantAttributeRepository = materialVariantAttributeRepository;
        }

        public async Task AddAsync(MaterialVariantAttribute materialVariantAttribute)
        {
            await _materialVariantAttributeRepository.AddAsync(materialVariantAttribute);
        }

        public async Task AddRange(IEnumerable<MaterialVariantAttribute> materialVariantAttributes)
        {
            await _materialVariantAttributeRepository.AddRangce(materialVariantAttributes);
        }

        public async Task<bool> CheckExist(Expression<Func<MaterialVariantAttribute, bool>> where)
        {
            return await _materialVariantAttributeRepository.CheckExist(where);
        }

        public async Task<MaterialVariantAttribute> FindAsync(Guid id)
        {
            return await _materialVariantAttributeRepository.FindAsync(id);
        }

        public IQueryable<MaterialVariantAttribute> Get(Expression<Func<MaterialVariantAttribute, bool>> where)
        {
            return _materialVariantAttributeRepository.Get(where);
        }

        public IQueryable<MaterialVariantAttribute> Get(Expression<Func<MaterialVariantAttribute, bool>> where, params Expression<Func<MaterialVariantAttribute, object>>[] includes)
        {
            return _materialVariantAttributeRepository.Get(where, includes);
        }

        public IQueryable<MaterialVariantAttribute> Get(Expression<Func<MaterialVariantAttribute, bool>> where, Func<IQueryable<MaterialVariantAttribute>, IIncludableQueryable<MaterialVariantAttribute, object>> include = null)
        {
            return _materialVariantAttributeRepository.Get(where, include);
        }

        public IQueryable<MaterialVariantAttribute> GetAll()
        {
            return _materialVariantAttributeRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _materialVariantAttributeRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(MaterialVariantAttribute materialVariantAttribute)
        {
            _materialVariantAttributeRepository.Update(materialVariantAttribute);
        }
    }
}
