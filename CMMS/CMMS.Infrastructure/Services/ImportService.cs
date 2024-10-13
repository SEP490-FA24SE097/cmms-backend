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
    public interface IImportService
    {
        Task<Import> FindAsync(Guid id);
        IQueryable<Import> GetAll();
        IQueryable<Import> Get(Expression<Func<Import, bool>> where);
        IQueryable<Import> Get(Expression<Func<Import, bool>> where, params Expression<Func<Import, object>>[] includes);
        IQueryable<Import> Get(Expression<Func<Import, bool>> where, Func<IQueryable<Import>, IIncludableQueryable<Import, object>> include = null);
        Task AddAsync(Import import);
        Task AddRange(IEnumerable<Import> imports);
        void Update(Import import);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Import, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class ImportService : IImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImportRepository _importRepository;

        public ImportService(IUnitOfWork unitOfWork, IImportRepository importRepository)
        {
            _unitOfWork = unitOfWork;
            _importRepository = importRepository;
        }

        public async Task AddAsync(Import import)
        {
            await _importRepository.AddAsync(import);
        }

        public async Task AddRange(IEnumerable<Import> imports)
        {
            await _importRepository.AddRangce(imports);
        }

        public async Task<bool> CheckExist(Expression<Func<Import, bool>> where)
        {
            return await _importRepository.CheckExist(where);
        }

        public async Task<Import> FindAsync(Guid id)
        {
            return await _importRepository.FindAsync(id);
        }

        public IQueryable<Import> Get(Expression<Func<Import, bool>> where)
        {
            return _importRepository.Get(where);
        }

        public IQueryable<Import> Get(Expression<Func<Import, bool>> where, params Expression<Func<Import, object>>[] includes)
        {
            return _importRepository.Get(where, includes);
        }

        public IQueryable<Import> Get(Expression<Func<Import, bool>> where, Func<IQueryable<Import>, IIncludableQueryable<Import, object>> include = null)
        {
            return _importRepository.Get(where, include);
        }

        public IQueryable<Import> GetAll()
        {
            return _importRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _importRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Import import)
        {
            _importRepository.Update(import);
        }
    }
}
