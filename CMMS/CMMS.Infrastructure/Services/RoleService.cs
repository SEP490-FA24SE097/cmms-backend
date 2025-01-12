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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;


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
        Task SeedingPermission();
        Task LinkRolePermission();
    }
    public class RoleService : IRoleService
    {
        private ILogger _logger;
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private IMapper _mapper;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITransaction _efTransaction;

        public RoleService(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration,
            RoleManager<IdentityRole> roleManager, IMapper mapper, ApplicationDbContext dbContext,
            IPermissionRepository permissionRepository, IRolePermissionRepository rolePermissionRepository,
            IUnitOfWork unitOfWork, IRoleRepository roleRepository,
            IUserRepository userRepository, ILogger<RoleService> logger, ITransaction transaction)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _unitOfWork = unitOfWork;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _efTransaction = transaction;
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
            var role = await _roleRepository.FindAsync(id);
            if (role != null)
            {
                role.Name = roleName;
                _roleRepository.Update(role);
                if (await _unitOfWork.SaveChangeAsync()) return 1;
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
            var userList = _userRepository.GetAll();
            var userTotal = userList.Select(_ => new UserRoles { Id = _.Id }).ToListAsync();
            var users = await userList.Select(_ => new UserRoles
            {
                Id = _.Id,
                UserName = _.UserName,
                Email = _.Email,
                FullName = _.FullName,
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
                if (_roleRepository.Get(r => r.Name.Equals(role.ToString())).FirstOrDefault() == null)
                {
                    await _roleRepository.AddAsync(new IdentityRole(role.ToString())
                    {
                        NormalizedName = role.ToString().ToUpper()
                    });
                }
            }
            var result =  await _unitOfWork.SaveChangeAsync();
            if (result)
                await _efTransaction.CommitAsync();
        }

        public async Task SeedingPermission()
        {
            foreach (Enums.PermissionName permission in Enum.GetValues(typeof(Enums.PermissionName)))
            {
                if (_permissionRepository.Get(p => p.Name.Equals(permission.ToString())).FirstOrDefault() == null)
                {
                    await _permissionRepository.AddAsync(new Core.Entities.Permission
                    {
                        Name = permission.ToString(),
                        Id = Guid.NewGuid().ToString(),
                    });
                }
            }
            var result = await _unitOfWork.SaveChangeAsync();
            if (result)
                await _efTransaction.CommitAsync();
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
            var seniorPermission = EnumHelpers.GetEnumValues<Enums.SeniorManagementPermission>();
            var storeManagerPermission = EnumHelpers.GetEnumValues<Enums.StoreManagerPermission>();
            var storeShipperPermission = EnumHelpers.GetEnumValues<Enums.StoreShipperPermission>();
            var saleStaffPermission = EnumHelpers.GetEnumValues<Enums.SaleStaffPermission>();
            var customerPermission = EnumHelpers.GetEnumValues<Enums.CustomerPermission>();
            #endregion

            var rolePermissionMapping = new Dictionary<Role, List<string>>()
            {
                {Role.Senior_Management,  seniorPermission},
                {Role.Store_Manager,  storeManagerPermission},
                {Role.Sale_Staff,  saleStaffPermission},
                {Role.Customer,  customerPermission},
                {Role.Shipper_Store, storeShipperPermission }
            };
            List<RolePermission> rolesPermissions = new List<RolePermission>();
            foreach (var roleMapping in rolePermissionMapping)
            {
                var roleName = roleMapping.Key.ToString();
                var role = _roleRepository.Get(r => r.Name.Equals(roleName)).FirstOrDefault();
                foreach (var permissions in roleMapping.Value)
                {
                    var permisison = _permissionRepository.Get(p => p.Name.Equals(permissions)).FirstOrDefault();
                    if (_rolePermissionRepository.Get(rp => rp.RoleId.Equals(role.Id)
                    && rp.PermissionId.Equals(permisison.Id)).FirstOrDefault() == null)
                    {
                        _logger.LogInformation($"Adding permission {permissions} to role {roleName}.");
                        rolesPermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permisison.Id,
                        });
                    }
                }
            }
            if (!rolesPermissions.IsNullOrEmpty()) { 
                await _rolePermissionRepository.AddRangce(rolesPermissions);
                var result = await _unitOfWork.SaveChangeAsync();
                if (result)
                    await _efTransaction.CommitAsync();
            }

        }
    }
}
