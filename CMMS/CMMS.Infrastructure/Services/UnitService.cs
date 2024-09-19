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
    public interface IUnitService
    {
        Task<Unit> FindAsync(Guid id);
        IQueryable<Unit> GetAll();
        IQueryable<Unit> Get(Expression<Func<Unit, bool>> where);
        IQueryable<Unit> Get(Expression<Func<Unit, bool>> where, params Expression<Func<Unit, object>>[] includes);
        IQueryable<Unit> Get(Expression<Func<Unit, bool>> where, Func<IQueryable<Unit>, IIncludableQueryable<Unit, object>> include = null);
        Task AddAsync(Unit unit);
        Task AddRange(IEnumerable<Unit> units);
        void Update(Unit unit);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Unit, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class UnitService : IUnitService
    {
        private IUnitOfWork _unitOfWork;
        private IUnitRepository _unitRepository;
        public UnitService(IUnitOfWork unitOfWork, IUnitRepository unitRepository)
        {
            _unitOfWork = unitOfWork;
            _unitRepository = unitRepository;
        }

        public async Task AddAsync(Unit unit)
        {
            await _unitRepository.AddAsync(unit);
        }



        public async Task AddRange(IEnumerable<Unit> units)
        {
            await _unitRepository.AddRangce(units);
        }

        public async Task<bool> CheckExist(Expression<Func<Unit, bool>> where)
        {
            return await _unitRepository.CheckExist(where);
        }

        public async Task<Unit> FindAsync(Guid id)
        {
            return await _unitRepository.FindAsync(id);
        }

        public IQueryable<Unit> Get(Expression<Func<Unit, bool>> where)
        {
            return _unitRepository.Get(where);
        }

        public IQueryable<Unit> Get(Expression<Func<Unit, bool>> where, params Expression<Func<Unit, object>>[] includes)
        {
            return _unitRepository.Get(where, includes);
        }

        public IQueryable<Unit> Get(Expression<Func<Unit, bool>> where, Func<IQueryable<Unit>, IIncludableQueryable<Unit, object>> include = null)
        {
            return _unitRepository.Get(where, include);
        }

        public IQueryable<Unit> GetAll()
        {
            return _unitRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _unitRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Unit unit)
        {
            _unitRepository.Update(unit);
        }
    }
}
