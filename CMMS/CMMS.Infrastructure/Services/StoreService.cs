using AutoMapper;
using Azure.Identity;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        #region CRUD 
        Task<Store> FindAsync(string id);
        IQueryable<Store> GetAll();
        IQueryable<Store> Get(Expression<Func<Store, bool>> where);
        IQueryable<Store> Get(Expression<Func<Store, bool>> where, params Expression<Func<Store, object>>[] includes);
        IQueryable<Store> Get(Expression<Func<Store, bool>> where, Func<IQueryable<Store>, IIncludableQueryable<Store, object>> include = null);
        Task AddAsync(Store Store);
        Task AddRange(IEnumerable<Store> Stores);
        void Update(Store Store);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<Store, bool>> where);
        Task<bool> SaveChangeAsync();
        #endregion
        string GenerateStoreId();

        Task<Message> AddNewStoreManagerAsync(UserDTO user);
        Task<Message> AddNewSaleStaffAsync(UserDTO user);
        Task<Message> AddNewShipperAsync(UserDTO user);
        Task<UserVM> GetSaleStaffInStore(string storeId);
        Task<UserVM> GetStoreManagerInStore(string storeId);

    }

    public class StoreService : IStoreService
    {
        private IUnitOfWork _unitOfWork;
        private IStoreRepository _storeRepository;
        private IMapper _mapper;
        private IUserRepository _userRepository;
        private UserManager<ApplicationUser> _userManager;

        public StoreService(IUnitOfWork unitOfWork,
            IStoreRepository storeRepository, IMapper mapper, 
            IUserRepository userRepository, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _storeRepository = storeRepository;
            _mapper = mapper;
            _userRepository = userRepository;
            _userManager = userManager;
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
            storeEntity.Id = GenerateStoreId();

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
            //var WasManagedStore = await StoreWasManaged(storeId);
            //if (WasManagedStore) {
            //    message.Content = store.Name + " was managed by another users";
            //    return message;
            //}

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



        #region CURD 
        public async Task AddAsync(Store Store)
        {
            await _storeRepository.AddAsync(Store);
        }

        public async Task AddRange(IEnumerable<Store> Stores)
        {
            await _storeRepository.AddRangce(Stores);
        }

        public Task<bool> CheckExist(Expression<Func<Store, bool>> where)
        {
            return _storeRepository.CheckExist(where);
        }

        public Task<Store> FindAsync(string id)
        {
            return _storeRepository.FindAsync(id);
        }


        public IQueryable<Store> Get(Expression<Func<Store, bool>> where)
        {
            return _storeRepository.Get(where);
        }

        public IQueryable<Store> Get(Expression<Func<Store, bool>> where, params Expression<Func<Store, object>>[] includes)
        {
            return _storeRepository.Get(where, includes);
        }

        public IQueryable<Store> Get(Expression<Func<Store, bool>> where, Func<IQueryable<Store>, IIncludableQueryable<Store, object>> include = null)
        {
            return _storeRepository.Get(where, include);
        }

        public IQueryable<Store> GetAll()
        {
            return _storeRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _storeRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Store Store)
        {
            _storeRepository.Update(Store);
        }

        public string GenerateStoreId()
        {
            var transactionTotal = _storeRepository.GetAll();
            string invoiceCode = $"CH{(transactionTotal.Count() + 1):D6}";
            return invoiceCode;
        }

        #endregion


        public async Task<Message> AddNewStoreManagerAsync(UserDTO model)
        {
            var message = new Message();
            var isDupplicate = await _userManager.FindByEmailAsync(model.Email);
            if (isDupplicate != null)
            {
                message.Content = "Tên email đã bị được sử dụng";
                message.StatusCode = 500;
                return message;
            } else if (model.StoreId == null)
            {
                message.Content = "Store Manager phải quản lí 1 cửa hàng";
                message.StatusCode = 500;
                return message;
            }
            var userList = _userRepository.Get(_ => _.Id.Contains("STM"));
            string userId = $"STM{(userList.Count() + 1):D6}";
            var user = _mapper.Map<ApplicationUser>(model);
            user.EmailConfirmed = true;
            user.Id = userId;
            IdentityResult result = null;
            message.Content = "Thất bại";
            message.StatusCode = 500;
            result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                message.Content = "Thêm mới quản lí cửa hàng thành công.";
                message.StatusCode = 200;
                await _userManager.AddToRoleAsync(user, Role.Store_Manager.ToString());
            }
       

            return message;
        }

        public async Task<Message> AddNewSaleStaffAsync(UserDTO model)
        {
            var isDupplicate = await _userManager.FindByEmailAsync(model.Email);
            var message = new Message();
            if(isDupplicate != null)
            {
                message.Content = "Tên email đã bị được sử dụng";
                message.StatusCode = 500;
                return message;
            } else if (model.StoreId == null)
            {
                message.Content = "Sale staff phải trực thuộc 1 cửa hàng";
                message.StatusCode = 500;
                return message;
            }
            var userList = _userRepository.Get(_ => _.Id.Contains("NVBH"));
            string userId = $"NVBH{(userList.Count() + 1):D6}";
            var user = _mapper.Map<ApplicationUser>(model);
            user.EmailConfirmed = true;
            user.Id = userId;
            IdentityResult result = null;
            result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                message.Content = "Thêm mới nhân viên bán hàng thành công.";
                message.StatusCode = 200;
                await _userManager.AddToRoleAsync(user, Role.Sale_Staff.ToString());
            }
            return message;
        }

        public async Task<Message> AddNewShipperAsync(UserDTO model)
        {

            var isDupplicate = await _userManager.FindByEmailAsync(model.Email);
            var message = new Message();
            if (isDupplicate != null)
            {
                message.Content = "Tên email đã bị được sử dụng";
                message.StatusCode = 500;
                return message;
            }
            else if (model.StoreId == null)
            {
                message.Content = "Store Shipper phải trực thuộc 1 cửa hàng";
                message.StatusCode = 500;
                return message;
            }
            var userList = _userRepository.Get(_ => _.Id.Contains("NVVC"));
            string userId = $"NVVC{(userList.Count() + 1):D6}";
            var user = _mapper.Map<ApplicationUser>(model);
            user.EmailConfirmed = true;
            user.Id = userId;
            IdentityResult result = null;
            result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                message.Content = "Thêm mới store shipper thành công.";
                message.StatusCode = 200;
                await _userManager.AddToRoleAsync(user, Role.Shipper_Store.ToString());
            }
            return message;
        }

        public Task<UserVM> GetSaleStaffInStore(string storeId)
        {
            throw new NotImplementedException();
        }

        public Task<UserVM> GetStoreManagerInStore(string storeId)
        {
            throw new NotImplementedException();
        }
    }
}
