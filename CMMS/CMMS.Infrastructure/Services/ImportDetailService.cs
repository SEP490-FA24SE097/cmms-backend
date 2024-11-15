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
    public interface IImportDetailService
    {
        Task<ImportDetail> FindAsync(Guid id);
        IQueryable<ImportDetail> GetAll();
        IQueryable<ImportDetail> Get(Expression<Func<ImportDetail, bool>> where);
        IQueryable<ImportDetail> Get(Expression<Func<ImportDetail, bool>> where, params Expression<Func<ImportDetail, object>>[] includes);
        IQueryable<ImportDetail> Get(Expression<Func<ImportDetail, bool>> where, Func<IQueryable<ImportDetail>, IIncludableQueryable<ImportDetail, object>> include = null);
        Task AddAsync(ImportDetail import);
        Task AddRange(IEnumerable<ImportDetail> imports);
        void Update(ImportDetail import);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<ImportDetail, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class ImportDetailService : IImportDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImportDetailRepository _importDetailRepository;

        public ImportDetailService(IUnitOfWork unitOfWork, IImportDetailRepository importDetailRepository)
        {
            _unitOfWork = unitOfWork;
            _importDetailRepository = importDetailRepository;
        }

        public async Task AddAsync(ImportDetail import)
        {
            await _importDetailRepository.AddAsync(import);
        }

        public async Task AddRange(IEnumerable<ImportDetail> imports)
        {
            await _importDetailRepository.AddRangce(imports);
        }

        public async Task<bool> CheckExist(Expression<Func<ImportDetail, bool>> where)
        {
            return await _importDetailRepository.CheckExist(where);
        }

        public async Task<ImportDetail> FindAsync(Guid id)
        {
            return await _importDetailRepository.FindAsync(id);
        }

        public IQueryable<ImportDetail> Get(Expression<Func<ImportDetail, bool>> where)
        {
            return _importDetailRepository.Get(where);
        }

        public IQueryable<ImportDetail> Get(Expression<Func<ImportDetail, bool>> where, params Expression<Func<ImportDetail, object>>[] includes)
        {
            return _importDetailRepository.Get(where, includes);
        }

        public IQueryable<ImportDetail> Get(Expression<Func<ImportDetail, bool>> where, Func<IQueryable<ImportDetail>, IIncludableQueryable<ImportDetail, object>> include = null)
        {
            return _importDetailRepository.Get(where, include);
        }

        public IQueryable<ImportDetail> GetAll()
        {
            return _importDetailRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _importDetailRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(ImportDetail import)
        {
            _importDetailRepository.Update(import);
        }
    }
}
