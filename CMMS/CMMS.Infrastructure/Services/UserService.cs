using AutoMapper;
using Azure.Core;
using CMMS.Core.Constant;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;

namespace CMMS.Infrastructure.Services
{
    public interface IUserService
    {
        #region CRUD
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
        #endregion
        Task<bool> ConfirmAccount(string email);
        Task<ApplicationUser> FindAsync(string customerId);
        ApplicationUser FindWithNoTracking(string customerId);
        Task<IdentityResult> UpdateAnsyc(ApplicationUser updateUser);
        Task<bool> IsEmailConfirmedAsync(ApplicationUser user);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
        string GenerateCustomerCode();
        Task<decimal> GetCustomerDiscountPercentAsync(decimal amount, string userId);
        Task<decimal> GetRevenueFromCustomer(string userId);
        Task<decimal> GetAllRevenueFromCustomer();
        decimal GetAllCustomerCurrentDebt();
        decimal GetCustomerCurrentDebt(string userId);
        decimal GetAllCustomerTotalSale();
        decimal GetCustomerTotalSale(string userId);
        decimal GetAllCustomerTotalSaleAfterRefund();
        decimal GetCustomerTotalSaleAfterRefund(string userId);
        decimal GetCustomerCurrentDeftAtTheLastTransaction(string transactionId, string userId);



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
        private readonly IInvoiceService _invoiceService;
        private readonly IMaterialService _materialService;
        private readonly IVariantService _variantService;
        private readonly ITransactionService _transactionService;
        private readonly IUserService _userService;

        public UserService(IUserRepository userRepository, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration,
            RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork, IMapper mapper,
             IInvoiceService invoiceService, IMaterialService materialService,
             IVariantService variantService, ITransactionService transactionService)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _invoiceService = invoiceService;
            _materialService = materialService;
            _variantService = variantService;
            _transactionService = transactionService;
        }


        #region CRUD
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
            user.Id = GenerateCustomerCode();
            IdentityResult result = null;
            if (model.Password != null)
                result = await _userManager.CreateAsync(user, model.Password);
            else
                result = await _userManager.CreateAsync(user);
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
            user.Id = GenerateCustomerCode();
            IdentityResult result = IdentityResult.Success;
            user.Id = GenerateCustomerCode();
            if (userCM.Password != null)
            {
                result = await _userManager.CreateAsync(user, userCM.Password);
            }
            else
            {
                // login by google EmailConfirmed is true
                user.EmailConfirmed = true;
                result = await _userManager.CreateAsync(user);
                var loginProviderInfo = new UserLoginInfo(userCM.LoginProvider, userCM.ProviderKey, userCM.ProviderDisplayName);
                result = await _userManager.AddLoginAsync(user, loginProviderInfo);
            }

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
                message.StatusCode = 201;
            }
            await _unitOfWork.SaveChangeAsync();
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

        public async Task<ApplicationUser> FindAsync(string customerId)
        {
            return await _userRepository.FindAsync(customerId);
        }

        public async Task<bool> IsEmailConfirmedAsync(ApplicationUser user)
        {
            return await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            return await _userManager.ResetPasswordAsync(user, token, newPassword);
        }

        public string GenerateCustomerCode()
        {
            var userTotal = _userRepository.GetAll();
            string userId = $"KH{(userTotal.Count() + 1):D6}";
            return userId;
        }

        #endregion

        #region customer tracking revenue

        public async Task<decimal> GetCustomerDiscountPercentAsync(decimal amount, string userId)
        {
            var customerDiscountPercent = float.Parse(_configuration["User:DiscountPercent:Customer"]);
            var agencyDiscountPercnet = float.Parse(_configuration["User:DiscountPercent:Agency"]);
            var user = await _userRepository.FindAsync(userId);
            if (user.Type.Equals(CustomerType.Agency))
                return (decimal)((float)amount * agencyDiscountPercnet);
            return (decimal)((float)amount * customerDiscountPercent);
        }

        public async Task<decimal> GetRevenueFromCustomer(string userId)
        {
            decimal revenueCustomer = 0;
            var invoices = _invoiceService.Get(_ => _.CustomerId.Equals(userId), _ => _.InvoiceDetails);
            foreach (var invoice in invoices)
            {
                decimal invoiceRevenue = 0;
                foreach (var invoiceDetail in invoice.InvoiceDetails)
                {
                    var lineTotal = invoiceDetail.LineTotal;
                    var material = await _materialService.FindAsync(invoiceDetail.MaterialId);
                    var costPrice = material.CostPrice * invoiceDetail.Quantity;
                    decimal itemRevenue = lineTotal - costPrice;

                    if (invoiceDetail.VariantId != null)
                    {
                        var variant = await _variantService.FindAsync((Guid)invoiceDetail.VariantId);
                        costPrice = variant.CostPrice * invoiceDetail.Quantity;
                        itemRevenue = lineTotal - costPrice;
                    }
                    invoiceRevenue += itemRevenue;
                }
                revenueCustomer += invoiceRevenue;
            }
            return revenueCustomer;
        }

        public async Task<decimal> GetAllRevenueFromCustomer()
        {
            decimal revenueCustomer = 0;
            var customerInvoices = _invoiceService.Get(_ => _.CustomerId != null, _ => _.InvoiceDetails, _ => _.Customer);
            foreach (var invoice in customerInvoices)
            {
                decimal invoiceRevenue = 0;
                foreach (var invoiceDetail in invoice.InvoiceDetails)
                {
                    var lineTotal = invoiceDetail.LineTotal;
                    var material = await _materialService.FindAsync(invoiceDetail.MaterialId);
                    var costPrice = material.CostPrice * invoiceDetail.Quantity;
                    decimal itemRevenue = lineTotal - costPrice;

                    if (invoiceDetail.VariantId != null)
                    {
                        var variant = await _variantService.FindAsync((Guid)invoiceDetail.VariantId);
                        costPrice = variant.CostPrice * invoiceDetail.Quantity;
                        itemRevenue = lineTotal - costPrice;
                    }
                    invoiceRevenue += itemRevenue;
                }
                revenueCustomer += invoiceRevenue;
            }
            return revenueCustomer;
        }

        public decimal GetAllCustomerCurrentDebt()
        {
            decimal customerDebt = 0;
            var customerInvoices = _transactionService.GetAll();
            foreach (var transaction in customerInvoices)
            {
                if (transaction.TransactionType.Equals((int)TransactionType.SaleItem))
                    customerDebt += transaction.Amount;
                else if (transaction.TransactionType.Equals((int)TransactionType.PurchaseDebtInvoice))
                    customerDebt -= transaction.Amount;
            }
            return customerDebt;
        }

        public decimal GetCustomerCurrentDebt(string userId)
        {
            decimal customerDebt = 0;
            var customerInvoices = _transactionService.Get(_ => _.CustomerId.Equals(userId));
            foreach (var transaction in customerInvoices)
            {
                if (transaction.TransactionType.Equals((int)TransactionType.SaleItem))
                    customerDebt += transaction.Amount;
                else if (transaction.TransactionType.Equals((int)TransactionType.PurchaseDebtInvoice))
                    customerDebt -= transaction.Amount;
            }
            return customerDebt;
        }

        public decimal GetAllCustomerTotalSale()
        {
            decimal currentTotalSale = 0;
            var customerInvoices = _transactionService.GetAll();
            foreach (var transaction in customerInvoices)
            {
                if (transaction.TransactionType.Equals((int)TransactionType.SaleItem)
                    || transaction.TransactionType.Equals((int)TransactionType.QuickSale))
                    currentTotalSale += transaction.Amount;

            }
            return currentTotalSale;
        }
        public decimal GetCustomerTotalSale(string userId)
        {
            decimal currentTotalSale = 0;
            var customerInvoices = _transactionService.Get(_ => _.CustomerId.Equals(userId));
            foreach (var transaction in customerInvoices)
            {
                if (transaction.TransactionType.Equals((int)TransactionType.SaleItem)
                    || transaction.TransactionType.Equals((int)TransactionType.QuickSale))
                    currentTotalSale += transaction.Amount;

            }
            return currentTotalSale;
        }


        public decimal GetAllCustomerTotalSaleAfterRefund()
        {
            decimal currentTotalSaleAfterRefund = 0;
            var customerInvoices = _transactionService.GetAll();
            foreach (var transaction in customerInvoices)
            {
                if (transaction.TransactionType.Equals((int)TransactionType.SaleItem)
                   || transaction.TransactionType.Equals((int)TransactionType.QuickSale))
                    currentTotalSaleAfterRefund += transaction.Amount;
                else if (transaction.TransactionType.Equals((int)TransactionType.RefundInvoice))
                    currentTotalSaleAfterRefund -= transaction.Amount;
            }
            return currentTotalSaleAfterRefund;
        }
        public decimal GetCustomerTotalSaleAfterRefund(string userId)
        {
            decimal currentTotalSaleAfterRefund = 0;
            var customerInvoices = _transactionService.Get(_ => _.CustomerId.Equals(userId));
            foreach (var transaction in customerInvoices)
            {
                if (transaction.TransactionType.Equals((int)TransactionType.SaleItem)
                       || transaction.TransactionType.Equals((int)TransactionType.QuickSale))
                    currentTotalSaleAfterRefund += transaction.Amount;
                else if (transaction.TransactionType.Equals((int)TransactionType.RefundInvoice))
                    currentTotalSaleAfterRefund -= transaction.Amount;
            }
            return currentTotalSaleAfterRefund;
        }

        public decimal GetCustomerCurrentDeftAtTheLastTransaction(string transactionId, string userId)
        {
            decimal customerDebt = 0;
            var currentTransaction = _transactionService.Get(_ => _.Id.Equals(transactionId)).FirstOrDefault();
            var customerInvoices = _transactionService.Get(_ => _.CustomerId.Equals(userId) && _.TransactionDate <= currentTransaction.TransactionDate);
            foreach (var transaction in customerInvoices)
            {
                if (transaction.TransactionType.Equals((int)TransactionType.SaleItem))
                    customerDebt += transaction.Amount;
                else if (transaction.TransactionType.Equals((int)TransactionType.PurchaseDebtInvoice))
                    customerDebt -= transaction.Amount;
            }
            return customerDebt;
        }

        public ApplicationUser FindWithNoTracking(string customerId)
        {
            return _userRepository.Get(_ => _.Id.Equals(customerId)).AsNoTracking().FirstOrDefault();
        }

        public async Task<IdentityResult> UpdateAnsyc(ApplicationUser updateUser)
        {
            return await _userManager.UpdateAsync(updateUser);
        }

        #endregion
    }
}
