using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services
{
    public interface IMaterialService
    {
        Task<Material> FindAsync(Guid id);
        Task<float?> GetWeight(Guid materialId, Guid? variantId);
        IQueryable<Material> GetAll();
        IQueryable<Material> Get(Expression<Func<Material, bool>> where);
        IQueryable<Material> Get(Expression<Func<Material, bool>> where, params Expression<Func<Material, object>>[] includes);
        IQueryable<Material> Get(Expression<Func<Material, bool>> where, Func<IQueryable<Material>, IIncludableQueryable<Material, object>> include = null);
        Task AddAsync(Material material);
        Task AddRange(IEnumerable<Material> materials);
        void Update(Material material);
        Task<bool> Remove(Guid id);
        Task<bool> CheckExist(Expression<Func<Material, bool>> where);
        Task<bool> SaveChangeAsync();
    }

    public class MaterialService : IMaterialService
    {
        private IUnitOfWork _unitOfWork;
        private IMaterialRepository _materialRepository;
        private IVariantService _variantService;
        public MaterialService(IUnitOfWork unitOfWork, IMaterialRepository materialRepository, IVariantService variantService)
        {
            _unitOfWork = unitOfWork;
            _materialRepository = materialRepository;
            _variantService = variantService;
        }

        #region CRUD
        public async Task AddAsync(Material material)
        {
            await _materialRepository.AddAsync(material);
        }



        public async Task AddRange(IEnumerable<Material> materials)
        {
            await _materialRepository.AddRangce(materials);
        }

        public async Task<bool> CheckExist(Expression<Func<Material, bool>> where)
        {
            return await _materialRepository.CheckExist(where);
        }

        public async Task<Material> FindAsync(Guid id)
        {
            return await _materialRepository.FindAsync(id);
        }

        public IQueryable<Material> Get(Expression<Func<Material, bool>> where)
        {
            return _materialRepository.Get(where);
        }

        public IQueryable<Material> Get(Expression<Func<Material, bool>> where, params Expression<Func<Material, object>>[] includes)
        {
            return _materialRepository.Get(where, includes);
        }

        public IQueryable<Material> Get(Expression<Func<Material, bool>> where, Func<IQueryable<Material>, IIncludableQueryable<Material, object>> include = null)
        {
            return _materialRepository.Get(where, include);
        }

        public IQueryable<Material> GetAll()
        {
            return _materialRepository.GetAll();
        }

        public async Task<bool> Remove(Guid id)
        {
            return await _materialRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Material material)
        {
            _materialRepository.Update(material);
        }
        #endregion
        public async Task<decimal?> GetConversionRate(Guid materialId, Guid? variantId)
        {
            if (variantId == null)
                return null;
            else
            {
                var variant = await _variantService.Get(x => x.Id == variantId).Include(x => x.ConversionUnit).FirstOrDefaultAsync();

                if (variant.ConversionUnitId == null)
                    return null;

                else
                {
                    return variant.ConversionUnit.ConversionRate;
                }
            }
        }
        public async Task<float?> GetWeight(Guid materialId, Guid? variantId)
        {
            if (variantId == null)
            {
                return Get(x => x.Id == materialId).Select(x => x.WeightValue).FirstOrDefault();
            }
            else
            {
                var conversionRate = await GetConversionRate(materialId, variantId);
                if (conversionRate != null)
                {
                    return Get(x => x.Id == materialId).Select(x => x.WeightValue).FirstOrDefault() / (float)conversionRate;
                }
                else
                {
                    return Get(x => x.Id == materialId).Select(x => x.WeightValue).FirstOrDefault();
                }
            }

        }
    }
}
