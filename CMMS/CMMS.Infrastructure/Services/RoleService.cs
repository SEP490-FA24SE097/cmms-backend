using AutoMapper;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IRoleService
    {
        Task<List<IdentityRole>> GetRole();
        Task<IdentityRole> GetRoleById(String id);
        Task<IdentityResult> CreateRole(String roleName);
        Task<int> UpdateRole(String roleName, String id);
        Task<IdentityResult> DeleteRole(String roleId);
        Task<String[]> GetUserRole(string userId);
        Task<IdentityResult> AddRoleUser(List<string> roleNames, String userId);
        Task<List<UserRolesVM>> GetListUsers();
    }
    public class RoleService : IRoleService
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private IMapper _mapper;
        private ApplicationDbContext _dbContext;
        public RoleService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration,
            RoleManager<IdentityRole> roleManager, IMapper mapper, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<List<IdentityRole>> GetRole()
        {
            return await _roleManager.Roles.OrderBy(_ => _.Name).ToListAsync();
        }

        public async Task<IdentityRole> GetRoleById(string id)
        {
            return await _roleManager.FindByIdAsync(id);

        }

        public async Task<IdentityResult> CreateRole(string roleName)
        {
            IdentityRole _roleName = new IdentityRole(roleName);
            return await _roleManager.CreateAsync(_roleName);
        }

        public async Task<int> UpdateRole(string roleName, string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                role.Name = roleName;
                _dbContext.Roles.Update(role);
                return await _dbContext.SaveChangesAsync();
            }
            return 0;
        }

        public async Task<IdentityResult> DeleteRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            return await _roleManager.DeleteAsync(role);
        }

        public async Task<String[]> GetUserRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return (await _userManager.GetRolesAsync(user)).ToArray<string>();
        }

        public async Task<IdentityResult> AddRoleUser(List<string> roleNames, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var userRoles = (await _userManager.GetRolesAsync(user)).ToArray<string>();
            var deleteRoles = userRoles.Where(r => !roleNames.Contains(r));
            var addRoles = roleNames.Where(r => !userRoles.Contains(r));
            var result = await _userManager.RemoveFromRolesAsync(user, deleteRoles);
            return result = await _userManager.AddToRolesAsync(user, addRoles);
        }

        public async Task<List<UserRolesVM>> GetListUsers()
        {
            var userTotal = await _dbContext.Users.Select(_ => new UserRoles { Id = _.Id }).ToListAsync();
            var users = await _dbContext.Users.Select(_ => new UserRoles
            {
                Id = _.Id,
                UserName = _.UserName,
                Email = _.Email,
                //isActive = _.isActive,
                FirstName = _.FirstName,
                LastName = _.LastName,
                Gender = _.Gender,
            }).ToListAsync();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                user.RolesName = roles.ToList<string>();
            }
            var result = users.Select(_ => _mapper.Map<UserRoles, UserRolesVM>(_));
            return result.ToList();
        }
    }
}
