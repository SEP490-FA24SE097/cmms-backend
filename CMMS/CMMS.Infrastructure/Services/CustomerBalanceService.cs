using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface ICustomerBalanceService
    {
        #region CURD
        Task<CustomerBalance> FindAsync(string id);
        IQueryable<CustomerBalance> GetAll();
        IQueryable<CustomerBalance> Get(Expression<Func<CustomerBalance, bool>> where);
        IQueryable<CustomerBalance> Get(Expression<Func<CustomerBalance, bool>> where, params Expression<Func<CustomerBalance, object>>[] includes);
        IQueryable<CustomerBalance> Get(Expression<Func<CustomerBalance, bool>> where, Func<IQueryable<CustomerBalance>, IIncludableQueryable<CustomerBalance, object>> include = null);
        Task AddAsync(CustomerBalance customerBalance);
        Task AddRange(IEnumerable<CustomerBalance> customerBalances);
        void Update(CustomerBalance customerBalance);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<CustomerBalance, bool>> where);
        Task<bool> SaveChangeAsync();
        #endregion
        CustomerBalanceVM GetCustomerBalanceById(string userId);
        List<CustomerBalanceVM> GetCustomerBalance();

    }
    public class CustomerBalanceService : ICustomerBalanceService
    {
        private readonly ICustomerBalanceRepository _customerBalanceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CustomerBalanceService(ICustomerBalanceRepository customerBalanceRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _customerBalanceRepository = customerBalanceRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        #region CURD 
        public async Task AddAsync(CustomerBalance customerBalance)
        {
             await _customerBalanceRepository.AddAsync(customerBalance);
        }

        public async Task AddRange(IEnumerable<CustomerBalance> customerBalances)
        {
            await _customerBalanceRepository.AddRangce(customerBalances);
        }

        public Task<bool> CheckExist(Expression<Func<CustomerBalance, bool>> where)
        {
            return _customerBalanceRepository.CheckExist(where);
        }

        public Task<CustomerBalance> FindAsync(string id)
        {
            return _customerBalanceRepository.FindAsync(id);
        }

        public IQueryable<CustomerBalance> Get(Expression<Func<CustomerBalance, bool>> where)
        {
            return _customerBalanceRepository.Get(where);
        }

        public IQueryable<CustomerBalance> Get(Expression<Func<CustomerBalance, bool>> where, params Expression<Func<CustomerBalance, object>>[] includes)
        {
            return _customerBalanceRepository.Get(where, includes);
        }

        public IQueryable<CustomerBalance> Get(Expression<Func<CustomerBalance, bool>> where, Func<IQueryable<CustomerBalance>, IIncludableQueryable<CustomerBalance, object>> include = null)
        {
            return _customerBalanceRepository.Get(where, include);
        }

        public IQueryable<CustomerBalance> GetAll()
        {
            return _customerBalanceRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _customerBalanceRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(CustomerBalance customerBalance)
        {
             _customerBalanceRepository.Update(customerBalance);
        }
        #endregion
        public List<CustomerBalanceVM> GetCustomerBalance()
        {
            List<CustomerBalanceVM> customerBalanceVM = new List<CustomerBalanceVM>();
            return customerBalanceVM;
        }
        public CustomerBalanceVM GetCustomerBalanceById(string userId)
        {
            var customerBalance = _customerBalanceRepository.Get(_ => _.CustomerId.Equals(userId), _ => _.Customer)
                .AsNoTracking().FirstOrDefault();
            var userVM = _mapper.Map<UserVM>(customerBalance.Customer);
            var result = _mapper.Map<CustomerBalanceVM>(customerBalance);
            result.UserVM = userVM;
            return result;
        }

    }

}
