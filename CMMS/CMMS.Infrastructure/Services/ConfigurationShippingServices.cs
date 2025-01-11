using CMMS.Core.Entities;
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
    public interface IConfigurationShippingServices
    {
        #region CURD CofigShipping
        Task<ConfigShipping> FindAsync(string id);
        IQueryable<ConfigShipping> GetAll();
        IQueryable<ConfigShipping> Get(Expression<Func<ConfigShipping, bool>> where);
        IQueryable<ConfigShipping> Get(Expression<Func<ConfigShipping, bool>> where, params Expression<Func<ConfigShipping, object>>[] includes);
        IQueryable<ConfigShipping> Get(Expression<Func<ConfigShipping, bool>> where, Func<IQueryable<ConfigShipping>, IIncludableQueryable<ConfigShipping, object>> include = null);
        Task AddAsync(ConfigShipping ConfigShipping);
        Task AddRange(IEnumerable<ConfigShipping> ConfigShippings);
        void Update(ConfigShipping ConfigShipping);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<ConfigShipping, bool>> where);
        Task<bool> SaveChangeAsync();
      
        #endregion
    }

    public class ConfigurationShippingServices : IConfigurationShippingServices
    {
        private IConfigShippingRepository _configShippingRepository;
        private IUnitOfWork _unitOfWork;

        public ConfigurationShippingServices(IConfigShippingRepository configShippingRepository,
            IUnitOfWork unitOfWork)
        {
            _configShippingRepository = configShippingRepository;
            _unitOfWork = unitOfWork;
        }

        #region CURD 
        public async Task AddAsync(ConfigShipping ConfigShipping)
        {
            await _configShippingRepository.AddAsync(ConfigShipping);
        }

        public async Task AddRange(IEnumerable<ConfigShipping> ConfigShippings)
        {
            await _configShippingRepository.AddRangce(ConfigShippings);
        }

        public Task<bool> CheckExist(Expression<Func<ConfigShipping, bool>> where)
        {
            return _configShippingRepository.CheckExist(where);
        }

        public Task<ConfigShipping> FindAsync(string id)
        {
            return _configShippingRepository.FindAsync(id);
        }

        public IQueryable<ConfigShipping> Get(Expression<Func<ConfigShipping, bool>> where)
        {
            return _configShippingRepository.Get(where);
        }

        public IQueryable<ConfigShipping> Get(Expression<Func<ConfigShipping, bool>> where, params Expression<Func<ConfigShipping, object>>[] includes)
        {
            return _configShippingRepository.Get(where, includes);
        }

        public IQueryable<ConfigShipping> Get(Expression<Func<ConfigShipping, bool>> where, Func<IQueryable<ConfigShipping>, IIncludableQueryable<ConfigShipping, object>> include = null)
        {
            return _configShippingRepository.Get(where, include);
        }

        public IQueryable<ConfigShipping> GetAll()
        {
            return _configShippingRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _configShippingRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(ConfigShipping ConfigShipping)
        {
            _configShippingRepository.Update(ConfigShipping);
        }
        #endregion
    }
}
