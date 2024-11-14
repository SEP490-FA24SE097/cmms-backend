using CMMS.Core.Entities;
using CMMS.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using CMMS.Infrastructure.Data;

namespace CMMS.Infrastructure.Services
{
    public interface IConversionUnitService
    {
        Task<ConversionUnit> FindAsync(Guid id);
        IQueryable<ConversionUnit> GetAll();
        IQueryable<ConversionUnit> Get(Expression<Func<ConversionUnit, bool>> where);
        IQueryable<ConversionUnit> Get(Expression<Func<ConversionUnit, bool>> where, params Expression<Func<ConversionUnit, object>>[] includes);
        IQueryable<ConversionUnit> Get(Expression<Func<ConversionUnit, bool>> where, Func<IQueryable<ConversionUnit>, IIncludableQueryable<ConversionUnit, object>> include = null);
        Task AddAsync(ConversionUnit conversionUnit);
        Task AddRange(IEnumerable<ConversionUnit> conversionUnits);
        void Update(ConversionUnit conversionUnit);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<ConversionUnit, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class ConversionUnitService : IConversionUnitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConversionUnitRepository _conversionUnitRepository;

        public ConversionUnitService(IUnitOfWork unitOfWork, IConversionUnitRepository conversionUnitRepository)
        {
            _unitOfWork = unitOfWork;
            _conversionUnitRepository = conversionUnitRepository;
        }

        public async Task AddAsync(ConversionUnit conversionUnit)
        {
            await _conversionUnitRepository.AddAsync(conversionUnit);
        }

        public async Task AddRange(IEnumerable<ConversionUnit> conversionUnits)
        {
            await _conversionUnitRepository.AddRangce(conversionUnits);
        }

        public async Task<bool> CheckExist(Expression<Func<ConversionUnit, bool>> where)
        {
            return await _conversionUnitRepository.CheckExist(where);
        }

        public async Task<ConversionUnit> FindAsync(Guid id)
        {
            return await _conversionUnitRepository.FindAsync(id);
        }

        public IQueryable<ConversionUnit> Get(Expression<Func<ConversionUnit, bool>> where)
        {
            return _conversionUnitRepository.Get(where);
        }

        public IQueryable<ConversionUnit> Get(Expression<Func<ConversionUnit, bool>> where, params Expression<Func<ConversionUnit, object>>[] includes)
        {
            return _conversionUnitRepository.Get(where, includes);
        }

        public IQueryable<ConversionUnit> Get(Expression<Func<ConversionUnit, bool>> where, Func<IQueryable<ConversionUnit>, IIncludableQueryable<ConversionUnit, object>> include = null)
        {
            return _conversionUnitRepository.Get(where, include);
        }

        public IQueryable<ConversionUnit> GetAll()
        {
            return _conversionUnitRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _conversionUnitRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(ConversionUnit conversionUnit)
        {
            _conversionUnitRepository.Update(conversionUnit);
        }
    }
}
