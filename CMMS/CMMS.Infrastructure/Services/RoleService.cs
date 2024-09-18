using AutoMapper;
using CMMS.API.Helpers;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


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
        Task SeedingRole();
        Task LinkRolePermission();
    }
    public class RoleService : IRoleService
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private IMapper _mapper;
        private ApplicationDbContext _dbContext;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration,
            RoleManager<IdentityRole> roleManager, IMapper mapper, ApplicationDbContext dbContext,
            IPermissionRepository permissionRepository, IRolePermissionRepository rolePermissionRepository,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _dbContext = dbContext;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _unitOfWork = unitOfWork;
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

        public async Task SeedingRole()
        {
            foreach (Role role in Enum.GetValues(typeof(Role)))
            {
                if (!await _roleManager.RoleExistsAsync(role.ToString()))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role.ToString()));
                }
            }
        }

        public async Task SeedingPermission()
        {
            foreach (Enums.Permission permission in Enum.GetValues(typeof(Enums.Permission)))
            {
                if (!await _permissionRepository.GetAll().AnyAsync(p => p.Name.Equals(permission.ToString())))
                {

                    await _permissionRepository.AddAsync(new Core.Entities.Permission
                    {
                        Name = permission.ToString(),
                        Id = Guid.NewGuid().ToString(),
                    });
                }
            }
            await _unitOfWork.SaveChangeAsync();
        }

        public List<string> getRolePermission<T>(T rolePermission)
        {
            List<string> rolePermissons = new List<string>();
            foreach (T role in Enum.GetValues(typeof(T)))
            {
                rolePermissons.Add(role.ToString());
            }
            return rolePermissons;
        }

   

        public async Task LinkRolePermission()
        {

            #region getEnumPermission
            var adminPermission = EnumHelpers.GetEnumValues<Enums.AdminPermission>();
            var seniorPermission = EnumHelpers.GetEnumValues<Enums.SeniorManagementPermission>();
            var storeManagerPermission = EnumHelpers.GetEnumValues<Enums.StoreManagerPermission>();
            var saleStaffPermission = EnumHelpers.GetEnumValues<Enums.SaleStaffPermission>();
            var warehousePermission = EnumHelpers.GetEnumValues<Enums.WarehouseStaffPermission>();
            var customerPermission = EnumHelpers.GetEnumValues<Enums.CustomerPermission>();
            #endregion

            var rolePermissionMapping = new Dictionary<Role, List<string>>()
            {
                {Role.Admin,  adminPermission},
                {Role.Senior_Management,  seniorPermission},
                {Role.Store_Manager,  adminPermission},
                {Role.Sale_Staff,  saleStaffPermission},
                {Role.Warehouse_Staff,  warehousePermission},
                {Role.Customer,  customerPermission},
            };



            foreach (var roleMapping in rolePermissionMapping)
            {
                var role = await _roleManager.FindByNameAsync(roleMapping.Key.ToString());
                foreach (var permissions in roleMapping.Value)
                {
                    var permisison = _permissionRepository.Get(p => p.Name.Equals(permissions)).FirstOrDefault();
                    if(!_rolePermissionRepository.GetAll().Any(rp => rp.RoleId.Equals(role.Id) 
                    && rp.PermissionId.Equals(permisison.Id)))
                    {
                        await _rolePermissionRepository.AddAsync(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permisison.Id,
                        });
                    }
                }
            }
            await _unitOfWork.SaveChangeAsync();
        }
    }
}
