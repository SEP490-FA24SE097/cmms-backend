using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IStoreMaterialImportRequestService
    {
        Task<StoreMaterialImportRequest> FindAsync(Guid id);
        IQueryable<StoreMaterialImportRequest> GetAll();
        IQueryable<StoreMaterialImportRequest> Get(Expression<Func<StoreMaterialImportRequest, bool>> where);
        Task AddAsync(StoreMaterialImportRequest request);
        Task AddRange(IEnumerable<StoreMaterialImportRequest> requests);
        void Update(StoreMaterialImportRequest request);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<StoreMaterialImportRequest, bool>> where);
        Task<bool> SaveChangeAsync();
    }
    public class StoreMaterialImportRequestService : IStoreMaterialImportRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreMaterialImportRequestRepository _importRequestRepository;

        public StoreMaterialImportRequestService(IUnitOfWork unitOfWork, IStoreMaterialImportRequestRepository importRequestRepository)
        {
            _unitOfWork = unitOfWork;
            _importRequestRepository = importRequestRepository;
        }

        public async Task AddAsync(StoreMaterialImportRequest request)
        {
            await _importRequestRepository.AddAsync(request);
        }

        public async Task AddRange(IEnumerable<StoreMaterialImportRequest> requests)
        {
            await _importRequestRepository.AddRangce(requests);
        }

        public async Task<bool> CheckExist(Expression<Func<StoreMaterialImportRequest, bool>> where)
        {
            return await _importRequestRepository.CheckExist(where);
        }

        public async Task<StoreMaterialImportRequest> FindAsync(Guid id)
        {
            return await _importRequestRepository.FindAsync(id);
        }

        public IQueryable<StoreMaterialImportRequest> Get(Expression<Func<StoreMaterialImportRequest, bool>> where)
        {
            return _importRequestRepository.Get(where);
        }

        public IQueryable<StoreMaterialImportRequest> GetAll()
        {
            return _importRequestRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _importRequestRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(StoreMaterialImportRequest request)
        {
            _importRequestRepository.Update(request);
        }
    }
}