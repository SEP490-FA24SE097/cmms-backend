using AutoMapper;
using Azure.Identity;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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
        Task<StoreVM> GetStoreById(string storeId);
        Task<Message> CreateNewStore(StoreCM store);
        Task<Message> UpdateStore(StoreDTO store);
        Task<Message> CloseStore(string storeId);
        IQueryable<Store> GetAllStore(StoreType status);
        Task<Message> AssignUserToManageStore(string userId, string storeId);
        Task<Message> RemoveUserManagedStore(string userId, string storeId);
        Task<Message> ManageStoreRotation(string userId, string storeId);
        Task<bool> StoreWasManaged(string storeId);

    }

    public class StoreService : IStoreService
    {
        private IUnitOfWork _unitOfWork;
        private IStoreRepository _storeRepository;
        private IMapper _mapper;
        private IUserRepository _userRepository;

        public StoreService(IUnitOfWork unitOfWork,
            IStoreRepository storeRepository, IMapper mapper, 
            IUserRepository userRepository)
        {
            _unitOfWork = unitOfWork;
            _storeRepository = storeRepository;
            _mapper = mapper;
            _userRepository = userRepository;
        }
        public Task<Message> CloseStore(string storeId)
        {
            throw new NotImplementedException();
        }

        public async Task<Message> CreateNewStore(StoreCM store)
        {
            var message = new Message();
            var isDuplicatedName = _storeRepository.Get(_ => _.Name.Equals(store.Name)).FirstOrDefault();
            if (isDuplicatedName != null)
            {
                message.Content = "Store name is exsited";
                return message;
            }

            var storeEntity = _mapper.Map<Store>(store);
            storeEntity.Id = Guid.NewGuid().ToString();

            await _storeRepository.AddAsync(storeEntity);
            var result = await _unitOfWork.SaveChangeAsync();
            if (result) message.Content = "Create new store succesfully";
            return message;
        }

        public IQueryable<Store> GetAllStore(StoreType storeType)
        {
            var stores = _storeRepository.GetAll().AsQueryable();
            if (storeType.Equals(StoreType.Available))
            {
                var listStoreWasManaged = _userRepository.Get(_ => _.StoreId != null, s => s.Store)
                    .Select(_ => _.Store);
                var listStoreAvailable = _storeRepository.GetAll().Except(listStoreWasManaged);
                return listStoreAvailable;
            } else if (storeType.Equals(StoreType.WasManaged))
            {
                var listStoreWasManaged = _userRepository.Get(_ => _.StoreId != null, s => s.Store)
                    .Select(_ => _.Store);
                return listStoreWasManaged;
            }
            return stores;
        }

        public async Task<Message> UpdateStore(StoreDTO store)
        {
            var message = new Message();
            var storeEntity = _storeRepository.Get(_ => _.Id.Equals(store.Id)).FirstOrDefault();
            if (storeEntity == null) { 
                message.Content = "Store not found";
                return message;
            }
            var isDuplicatedName = _storeRepository.Get(_ => _.Name.Equals(store.Name)).FirstOrDefault();
            if (isDuplicatedName != null)
            {
                message.Content = "Store name is duplicated";
                return message;
            }

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
            var result = await _unitOfWork.SaveChangeAsync();
            if (result) message.Content = "Update store " + store.Id + " sucesfully";
            return message;

        }

        public async Task<Message> AssignUserToManageStore(string userId, string storeId)
        {
            var message = new Message();
            var user = await _userRepository.FindAsync(userId);
            var store = await _storeRepository.FindAsync(storeId);
            if (user == null)
            {
                message.Content = "User not found";
                return message;
            } else if (store == null) {
                message.Content = "Store not found";
                return message;
            }
            var WasManagedStore = await StoreWasManaged(storeId);
            if (WasManagedStore) {
                message.Content = store.Name + " was managed by another users";
                return message;
            }

            user.StoreId = storeId;
            _userRepository.Update(user);
            await _unitOfWork.SaveChangeAsync();
            message.Content = "Assign user " + user.FullName + " to managed " + store.Name + " succesffulyy";
            return message;


        }

        public async Task<Message> RemoveUserManagedStore(string userId, string storeId)
        {
            var message = new Message();
            var user = await _userRepository.FindAsync(userId);
            var store = await _storeRepository.FindAsync(storeId);
            if (user == null)
            {
                message.Content = "User not found";
                return message;
            }
            else if (store == null)
            {
                message.Content = "Store not found";
                return message;
            }
            user.StoreId = null;
            _userRepository.Update(user);   
            await _unitOfWork.SaveChangeAsync();
            message.Content = "Removed user " + user.UserName + " from managed store " + store.Name;
            return message;
        }

        public async Task<Message> ManageStoreRotation(string userId, string storeId)
        {
            var isStoreManaged = await StoreWasManaged(storeId);

            if (isStoreManaged) {
                // remove old manager
                var user = _userRepository.Get(_ => _.StoreId.Equals(storeId)).FirstOrDefault();
                user.StoreId = null;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangeAsync();
                // set store id to new manager
                return await AssignUserToManageStore(userId, storeId);
               
            }
            return await AssignUserToManageStore(userId, storeId);
        }

        public async Task<bool> StoreWasManaged(string storeId)
        {
            var isManaged = await _userRepository.Get(s => s.StoreId.Equals(storeId)).FirstOrDefaultAsync();
            if(isManaged == null)
            {
                return false;
            }
            return true; 
        }

        public async Task<StoreVM> GetStoreById(string storeId)
        {
            var store = await _storeRepository.FindAsync(storeId);
            if(store != null)
            {
                var storeVM = _mapper.Map<StoreVM>(store);  
                var user = _userRepository.Get(_ => _.StoreId.Equals(storeId)).FirstOrDefault();
                if (user != null) {
                    storeVM.Manager = _mapper.Map<UserVM>(user);
                }
                return storeVM;
            }
            return null;

        }
    }
}
