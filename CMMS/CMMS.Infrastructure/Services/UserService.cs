using AutoMapper;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
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
        //Task<IdentityResult> SignUpAsync(UserDTO model);
        //Task<ApplicationUser> SignInAsync(UserSignIn model);
        Task<IList<String>> GetRolesAsync(ApplicationUser user);

        Task<ApplicationUser> FindAsync(Guid id);
        Task<ApplicationUser> FindbyEmail(String email);
        Task<ApplicationUser> FindByUserName(String userName);
        Task<IQueryable<UserRolesVM>> GetAll();
        IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where);
        IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where, params Expression<Func<ApplicationUser, object>>[] includes);
        IQueryable<ApplicationUser> Get(Expression<Func<ApplicationUser, bool>> where, Func<IQueryable<ApplicationUser>, IIncludableQueryable<ApplicationUser, object>> include = null);
        //Task<Message> AddAsync(UserDTO user);
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

        //public async Task<IdentityResult> SignUpAsync(UserDTO model)
        //{
        //    var isDupplicate = await _userManager.FindByEmailAsync(model.Email);
        //    if (isDupplicate != null)
        //    {
        //        return null;
        //    }
        //    var user = _mapper.Map<ApplicationUser>(model);

        //    var result = await _userManager.CreateAsync(user, model.Password);
        //    if (result.Succeeded)
        //    {
        //        if (!await _roleManager.RoleExistsAsync(AppRole.Customer))
        //        {
        //            await _roleManager.CreateAsync(new IdentityRole(AppRole.Customer));
        //        }

        //        if (!await _roleManager.RoleExistsAsync(AppRole.Admin))
        //        {
        //            await _roleManager.CreateAsync(new IdentityRole(AppRole.Admin));
        //        }
        //        if (model.IsAdmin)
        //        {
        //            await _userManager.AddToRoleAsync(user, AppRole.Admin);
        //        }
        //        else
        //        {
        //            await _userManager.AddToRoleAsync(user, AppRole.Customer);
        //        }

        //        //await _userManager.AddToRoleAsync(user, AppRole.Customer);
        //    }

        //    return result;
        //}

        //public async Task<Message> AddAsync(UserDTO userVM)
        //{
        //    var isDupplicate = await _userManager.FindByEmailAsync(userVM.Email);
        //    if (isDupplicate != null)
        //    {
        //        return null;
        //    }
        //    var user = _mapper.Map<ApplicationUser>(userVM);
        //    var result = await _userManager.CreateAsync(user, userVM.Password);
        //    if (result.Succeeded)
        //    {
        //        if (!await _roleManager.RoleExistsAsync(AppRole.Customer))
        //        {
        //            await _roleManager.CreateAsync(new IdentityRole(AppRole.Customer));
        //        }

        //        if (!await _roleManager.RoleExistsAsync(AppRole.Admin))
        //        {
        //            await _roleManager.CreateAsync(new IdentityRole(AppRole.Admin));
        //        }
        //        if (userVM.IsAdmin)
        //        {
        //            await _userManager.AddToRoleAsync(user, AppRole.Admin);
        //        }
        //        else
        //        {
        //            await _userManager.AddToRoleAsync(user, AppRole.Customer);
        //        }
        //    }

        //    return new Message
        //    {
        //        StatusCode = 400,
        //        Content = "Create new user successfully",
        //    };
        //}

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
                //if (userRoles.Contains(AppRole.Admin))
                //{
                //    listUser.Remove(user);
                //}
                //else
                //{
                //    var userRolesVM = _mapper.Map<UserRolesVM>(user);
                //    userRolesVM.RolesName = userRoles.ToList();
                //    listUserRolesVM.Add(userRolesVM);
                //}
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
