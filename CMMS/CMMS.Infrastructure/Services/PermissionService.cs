using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IPermissionSerivce
    {
        List<Permission> GetAll();
        Task<IdentityRole> GetPermissionById(String id);
        Task<IdentityResult> CreatePermission(String permission);
        Task<IdentityResult> DeletePermission(String permissionId);
        Task<String[]> GetUserPermission(string userId);
        Task<RolePermissions> GetRolePermision(string roleId);
        Task<IdentityResult> AddUserPermission(String userId,  String permissionId);
        Task<IdentityResult> RemoveUserPermission(String userId,  String permissionId);

    }
    public class PermissionService : IPermissionSerivce
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _iUnitOfWork;
        private readonly IUserPermisisonRepository _userPermissionRepository;
       

        public PermissionService(IPermissionRepository permissionRepository, 
            IUserPermisisonRepository userPermisisonRepository,
            IUnitOfWork unitOfWork)
        {
            _userPermissionRepository = userPermisisonRepository;
            _permissionRepository = permissionRepository;
            _iUnitOfWork = unitOfWork;
        }

      

        public Task<IdentityResult> CreatePermission(string permission)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> DeletePermission(string permissionId)
        {
            throw new NotImplementedException();
        }

        public List<Permission> GetAll()
        {
            return  _permissionRepository.GetAll().ToList();
        }

        public Task<IdentityRole> GetPermissionById(string id)
        {
            throw new NotImplementedException();
        }

        public Task<RolePermissions> GetRolePermision(string roleId)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetUserPermission(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RemoveUserPermission(string userId, string permissionId)
        {
            var permission = _permissionRepository.Get(_ => _.Id.Equals(permissionId)).FirstOrDefault();
            if (permission == null) throw new Exception("Permission not found");

            var userPermission = new UserPermission
            {
                UserId = userId,
                PermissionId = permission.Id,
            };
            _userPermissionRepository.Remove(userPermission);
            return await _iUnitOfWork.SaveChangeAsync();
        }

        public async Task<bool> AddUserPermission(string userId, string permissionId)
        {
            var permission = _permissionRepository.Get(_ => _.Id.Equals(permissionId)).FirstOrDefault();
            if (permission == null) throw new Exception("Permission not found");

            var userPermission = new UserPermission
            {
                UserId = userId,
                PermissionId = permission.Id,
            };
            await _userPermissionRepository.AddAsync(userPermission);
            return await _iUnitOfWork.SaveChangeAsync();

        }
    }
}
