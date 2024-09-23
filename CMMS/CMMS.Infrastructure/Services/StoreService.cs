using AutoMapper;
using Azure.Identity;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IStoreService
    {
        Task CreateNewStore(StoreCM store);
        Task UpdateStore(StoreDTO store);
        Task CloseStore(string storeId);
        IQueryable<Store> GetAllStore();
    }

    public class StoreService : IStoreService
    {
        private IUnitOfWork _unitOfWork;
        private IStoreRepository _storeRepository;
        private IMapper _mapper;

        public StoreService(IUnitOfWork unitOfWork,
            IStoreRepository storeRepository, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _storeRepository = storeRepository;
            _mapper = mapper;
        }
        public Task CloseStore(string storeId)
        {
            throw new NotImplementedException();
        }

        public async Task CreateNewStore(StoreCM store)
        {
            var response = new Response();  
            var isDuplicatedName = _storeRepository.Get(_ => _.Name.Equals(store.Name)).FirstOrDefault();
            if (isDuplicatedName != null)
            {
                response.ResponseMessage = "Store name is exsited";
            }

            var storeEntity = _mapper.Map<Store>(store);
            storeEntity.Id = Guid.NewGuid().ToString();

            await _storeRepository.AddAsync(storeEntity);
            var result = await _unitOfWork.SaveChangeAsync();
          
        }

        public IQueryable<Store> GetAllStore()
        {
            var stores = _storeRepository.GetAll().AsQueryable();
            return stores;
        }

        public async Task UpdateStore(StoreDTO store)
        {
            var response = new Response();
            var storeEntity = _storeRepository.Get(_ => _.Id.Equals(store.Id)).FirstOrDefault();
            if(storeEntity == null) response.ResponseMessage = "Store not found";
            var isDuplicatedName = _storeRepository.Get(_ => _.Name.Equals(store.Name)).FirstOrDefault();
            if (isDuplicatedName != null) response.ResponseMessage = "Store name is exsited";

            var updateStoreEntity = new Store
            {
                Id = store.Id,
                Name = store.Name,
                Address = store.Address,
                District = store.District,
                Phone = store.Phone,
                Province = store.Province,
                Status = store.Status,
                Ward = store.Ward,
            };

            _storeRepository.Update(updateStoreEntity);
            await _unitOfWork.SaveChangeAsync();

        }
    }
}
