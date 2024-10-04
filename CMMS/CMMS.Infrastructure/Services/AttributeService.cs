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
using Attribute = CMMS.Core.Entities.Attribute;

namespace CMMS.Infrastructure.Services
{
    public interface IAttributeService
    {
        Task<Attribute> FindAsync(Guid id);
        IQueryable<Attribute> GetAll();
        IQueryable<Attribute> Get(Expression<Func<Attribute, bool>> where);
        IQueryable<Attribute> Get(Expression<Func<Attribute, bool>> where, params Expression<Func<Attribute, object>>[] includes);
        IQueryable<Attribute> Get(Expression<Func<Attribute, bool>> where, Func<IQueryable<Attribute>, IIncludableQueryable<Attribute, object>> include = null);
        Task AddAsync(Attribute attribute);
        Task AddRange(IEnumerable<Attribute> attributes);
        void Update(Attribute attribute);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Attribute, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class AttributeService : IAttributeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAttributeRepository _attributeRepository;

        public AttributeService(IUnitOfWork unitOfWork, IAttributeRepository attributeRepository)
        {
            _unitOfWork = unitOfWork;
            _attributeRepository = attributeRepository;
        }

        public async Task AddAsync(Attribute attribute)
        {
            await _attributeRepository.AddAsync(attribute);
        }

        public async Task AddRange(IEnumerable<Attribute> attributes)
        {
            await _attributeRepository.AddRangce(attributes);
        }

        public async Task<bool> CheckExist(Expression<Func<Attribute, bool>> where)
        {
            return await _attributeRepository.CheckExist(where);
        }

        public async Task<Attribute> FindAsync(Guid id)
        {
            return await _attributeRepository.FindAsync(id);
        }

        public IQueryable<Attribute> Get(Expression<Func<Attribute, bool>> where)
        {
            return _attributeRepository.Get(where);
        }

        public IQueryable<Attribute> Get(Expression<Func<Attribute, bool>> where, params Expression<Func<Attribute, object>>[] includes)
        {
            return _attributeRepository.Get(where, includes);
        }

        public IQueryable<Attribute> Get(Expression<Func<Attribute, bool>> where, Func<IQueryable<Attribute>, IIncludableQueryable<Attribute, object>> include = null)
        {
            return _attributeRepository.Get(where, include);
        }

        public IQueryable<Attribute> GetAll()
        {
            return _attributeRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _attributeRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Attribute attribute)
        {
            _attributeRepository.Update(attribute);
        }
    }
}
