using AutoMapper;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
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
    public interface IUserService
    {
        Task<IdentityResult> CustomerSignUpAsync(UserDTO model);
        Task<ApplicationUser> SignInAsync(UserSignIn model);
        Task<Message> AddAsync(UserCM user);
        Task<IList<String>> GetRolesAsync(ApplicationUser user);
        Task<ApplicationUser> FindAsync(Guid id);
        Task<ApplicationUser> FindbyEmail(String email);
        Task<ApplicationUser> FindByUserName(String userName);
        Task<IQueryable<UserRolesVM>> GetAll();
        IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where);
        IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where, params Expression<Func<ApplicationUser, object>>[] includes);
        IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where, Func<IQueryable<ApplicationUser>, IIncludableQueryable<ApplicationUser, object>> include = null);
        void Update(ApplicationUser user);
        Task<bool> CheckExist(Expression<Func<ApplicationUser, bool>> where);
        Task<bool> SaveChangeAsync();
        Task<bool> ConfirmAccount(string email);
    }
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UserService(IUserRepository userRepository, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration,
            RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApplicationUser> SignInAsync(UserSignIn model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                return user;
            }
            return null;
        }

        public async Task<IdentityResult> CustomerSignUpAsync(UserDTO model)
        {
            var isDupplicate = await _userManager.FindByEmailAsync(model.Email);
            if (isDupplicate != null)
            {
                return null;
            }
            var user = _mapper.Map<ApplicationUser>(model);

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                    await _userManager.AddToRoleAsync(user, Role.Customer.ToString());
            }

            return result;
        }

        public async Task<Message> AddAsync(UserCM userCM)
        {
            var message = new Message();
            var isDupplicate = await _userManager.FindByEmailAsync(userCM.Email);
            if (isDupplicate != null)
            {
                message.Content = "Email is already in use";
                return message;
            }
            var user = _mapper.Map<ApplicationUser>(userCM);
            var result = await _userManager.CreateAsync(user, userCM.Password);
            if (result.Succeeded)
            {
                var roleName = userCM.RoleName;
                var isExistedRole = await _roleManager.FindByNameAsync(roleName);
                if (isExistedRole == null)
                {
                    message.Content = "Role not found";
                    return message;
                }
                else await _userManager.AddToRoleAsync(user, roleName);
                message.Content = "Add new user successfully";
            }
            return message;
        }

        public async Task<bool> CheckExist(Expression<Func<ApplicationUser, bool>> where)
        {
            return await _userRepository.CheckExist(where);
        }

        public async Task<ApplicationUser> FindAsync(Guid id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where)
        {
            return _userRepository.Get(where);
        }

        public IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where, params Expression<Func<ApplicationUser, object>>[] includes)
        {
            return _userRepository.Get(where, includes);
        }

        public IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where, Func<IQueryable<ApplicationUser>, IIncludableQueryable<ApplicationUser, object>> include = null)
        {
            return _userRepository.Get(where, include);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(ApplicationUser user)
        {
            _userRepository.Update(user);
        }

        public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IQueryable<UserRolesVM>> GetAll()
        {
            var listUserRolesVM = new List<UserRolesVM>();
            var listUser = _userRepository.GetAll().ToList();
            foreach (var user in listUser.ToList())
            {
                var userRoles = (await GetRolesAsync(user));
                var userRolesVM = _mapper.Map<UserRolesVM>(user);
                userRolesVM.RolesName = userRoles.ToList();
                listUserRolesVM.Add(userRolesVM);
                if (userRoles.Contains(Role.Senior_Management.ToString()))
                {
                    listUser.Remove(user);
                }
            }
            return listUserRolesVM.AsQueryable();
        }

        public async Task<ApplicationUser> FindbyEmail(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser> FindByUserName(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<bool> ConfirmAccount(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            user.EmailConfirmed = true;
            _userRepository.Update(user);
            return await _unitOfWork.SaveChangeAsync();
        }
    }
}
