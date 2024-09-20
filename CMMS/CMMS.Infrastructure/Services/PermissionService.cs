using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IPermissionSerivce
    {
        IQueryable<Permission> GetAll();
        //Task<Permission> GetPermissionById(String id);
        Task<bool> CreatePermission(String permission);
        Task<bool> DeletePermission(String permissionId);
        Task<string[]> GetUserPermission(string userId);
        Task<RolePermissions> GetRolePermision(string roleId);
        Task<bool> AddUserPermission(String userId,  String permissionId);
        Task<bool> RemoveUserPermission(String userId,  String permissionId);

    }
    public class PermissionService : IPermissionSerivce
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _iUnitOfWork;
        private readonly IRoleRepository _roleRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPermisisonRepository _userPermissionRepository;
       

        public PermissionService(IPermissionRepository permissionRepository, 
            IUserPermisisonRepository userPermisisonRepository,
            IUnitOfWork unitOfWork, 
            IRoleRepository roleRepository, IRolePermissionRepository rolePermissionRepository,
            UserManager<ApplicationUser> userManager)
        {
            _userPermissionRepository = userPermisisonRepository;
            _permissionRepository = permissionRepository;
            _iUnitOfWork = unitOfWork;
            _roleRepository = roleRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _userManager = userManager;
        }


        public async Task<bool> CreatePermission(string permissionName)
        {
            var permission = new Permission
            {
                Id = Guid.NewGuid().ToString(),
                Name = permissionName,
            };
            await _permissionRepository.AddAsync(permission);
            return await _iUnitOfWork.SaveChangeAsync();
        }

        public async Task<bool> DeletePermission(string permissionId)
        {
            var permission = await _permissionRepository.FindAsync(permissionId);
             _permissionRepository.Remove(permission);
            return await _iUnitOfWork.SaveChangeAsync();
        }

        public IQueryable<Permission> GetAll()
        {
            return  _permissionRepository.GetAll();
        }

        public Permission GetPermissionById(string id)
        {
            return _permissionRepository.Get(_ => _.Id.Equals(id)).FirstOrDefault(); 
        }

        public async Task<RolePermissions> GetRolePermision(string roleId)
        {
            var role = _roleRepository.Get(_ => _.Id.Equals(roleId)).FirstOrDefault();
            if (role == null) throw new Exception("Role not found");

            var permission = _rolePermissionRepository.Get(_ => _.RoleId.Equals(role.Id), _ => _.Permission);
            string[] permissionNames = await permission.Select(_ => _.Permission.Name).ToArrayAsync();
            RolePermissions rolePermissions = new RolePermissions
            {
                RoleName = role.Name,
                Permissions = permissionNames
            };
            return rolePermissions;

        }

        public async Task<string[]> GetUserPermission(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var roleNames = await _userManager.GetRolesAsync(user);

            var roleIds = new List<string>();

            foreach (var roleName in roleNames)
            {
                var role =  _roleRepository.Get(r => r.Name.Equals(roleName)).FirstOrDefault();
                if (role != null)
                {
                    roleIds.Add(role.Id);  // Add RoleId to the list
                }
            }

            var roleId = roleIds.FirstOrDefault();

            var userPermissions =  _userPermissionRepository.Get(_ => _.UserId.Equals(userId), _ => _.Permission)
                .Select(_ => _.Permission.Name);

            var rolePermissions =  _rolePermissionRepository.Get(_ => _.RoleId.Equals(roleId), _ => _.Permission)
                .Select(_ => _.Permission.Name);
            var result = await userPermissions.Concat(rolePermissions).Distinct().ToArrayAsync();
            return result;
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
