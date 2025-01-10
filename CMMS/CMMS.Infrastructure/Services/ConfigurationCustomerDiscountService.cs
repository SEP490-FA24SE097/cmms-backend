using CMMS.Core.Entities.Configurations;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface IConfigurationCustomerDiscountService
    {
        #region CURD CofigShipping
        Task<ConfigCustomerDiscount> FindAsync(string id);
        IQueryable<ConfigCustomerDiscount> GetAll();
        IQueryable<ConfigCustomerDiscount> Get(Expression<Func<ConfigCustomerDiscount, bool>> where);
        IQueryable<ConfigCustomerDiscount> Get(Expression<Func<ConfigCustomerDiscount, bool>> where, params Expression<Func<ConfigCustomerDiscount, object>>[] includes);
        IQueryable<ConfigCustomerDiscount> Get(Expression<Func<ConfigCustomerDiscount, bool>> where, Func<IQueryable<ConfigCustomerDiscount>, IIncludableQueryable<ConfigCustomerDiscount, object>> include = null);
        Task AddAsync(ConfigCustomerDiscount ShippingDetail);
        Task AddRange(IEnumerable<ConfigCustomerDiscount> ShippingDetails);
        void Update(ConfigCustomerDiscount ShippingDetail);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<ConfigCustomerDiscount, bool>> where);
        Task<bool> SaveChangeAsync();

        #endregion
    }

    public class ConfigurationCustomerDiscountService : IConfigurationCustomerDiscountService
    {
        private IConfigCustomerDiscountRepository _configCustomerDiscountRepository;
        private IUnitOfWork _unitOfWork;

        public ConfigurationCustomerDiscountService(IConfigCustomerDiscountRepository configCustomerDiscountRepository,
            IUnitOfWork unitOfWork)
        {
            _configCustomerDiscountRepository = configCustomerDiscountRepository;
            _unitOfWork = unitOfWork;
        }

        #region CURD 
        public async Task AddAsync(ConfigCustomerDiscount ConfigCustomerDiscount)
        {
            await _configCustomerDiscountRepository.AddAsync(ConfigCustomerDiscount);
        }

        public async Task AddRange(IEnumerable<ConfigCustomerDiscount> ConfigCustomerDiscounts)
        {
            await _configCustomerDiscountRepository.AddRangce(ConfigCustomerDiscounts);
        }

        public Task<bool> CheckExist(Expression<Func<ConfigCustomerDiscount, bool>> where)
        {
            return _configCustomerDiscountRepository.CheckExist(where);
        }

        public Task<ConfigCustomerDiscount> FindAsync(string id)
        {
            return _configCustomerDiscountRepository.FindAsync(id);
        }

        public IQueryable<ConfigCustomerDiscount> Get(Expression<Func<ConfigCustomerDiscount, bool>> where)
        {
            return _configCustomerDiscountRepository.Get(where);
        }

        public IQueryable<ConfigCustomerDiscount> Get(Expression<Func<ConfigCustomerDiscount, bool>> where, params Expression<Func<ConfigCustomerDiscount, object>>[] includes)
        {
            return _configCustomerDiscountRepository.Get(where, includes);
        }

        public IQueryable<ConfigCustomerDiscount> Get(Expression<Func<ConfigCustomerDiscount, bool>> where, Func<IQueryable<ConfigCustomerDiscount>, IIncludableQueryable<ConfigCustomerDiscount, object>> include = null)
        {
            return _configCustomerDiscountRepository.Get(where, include);
        }

        public IQueryable<ConfigCustomerDiscount> GetAll()
        {
            return _configCustomerDiscountRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _configCustomerDiscountRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(ConfigCustomerDiscount ConfigCustomerDiscount)
        {
            _configCustomerDiscountRepository.Update(ConfigCustomerDiscount);
        }
        #endregion
    }
}
